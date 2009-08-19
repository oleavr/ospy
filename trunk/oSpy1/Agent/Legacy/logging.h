/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

#pragma once

#include "softwall.h"

#define MAX_ELEMENTS        2048
#define PACKET_BUFSIZE     65536
#define MAX_SOFTWALL_RULES   128
#define BACKTRACE_BUFSIZE    384 /*
                                  * (sizeof(caller_module_name) + sizeof("::") + sizeof("0x12345678") * NUM_BT_LINES + SIZE_NUL_BYTE)
                                  *    rounded up to the closest "magic" size
                                  */

typedef enum MessageType {
  MESSAGE_TYPE_MESSAGE = 0,
  MESSAGE_TYPE_PACKET  = 1,
};

typedef enum MessageContext {
  MESSAGE_CTX_INFO                 =  0,
  MESSAGE_CTX_WARNING              =  1,
  MESSAGE_CTX_ERROR                =  2,
  MESSAGE_CTX_SOCKET_LISTENING     =  3,
  MESSAGE_CTX_SOCKET_CONNECTING    =  4,
  MESSAGE_CTX_SOCKET_CONNECTED     =  5,
  MESSAGE_CTX_SOCKET_DISCONNECTED  =  6,
  MESSAGE_CTX_SOCKET_RESET         =  7,
  MESSAGE_CTX_ACTIVESYNC_DEVICE    =  8,
  MESSAGE_CTX_ACTIVESYNC_STATUS    =  9,
  MESSAGE_CTX_ACTIVESYNC_SUBSTATUS = 10,
  MESSAGE_CTX_ACTIVESYNC_WZ_STATUS = 11,
};

typedef enum PacketDirection {
  PACKET_DIRECTION_INVALID  = 0,
  PACKET_DIRECTION_INCOMING = 1,
  PACKET_DIRECTION_OUTGOING = 2,
};

typedef struct {
  /* Common fields */
  SYSTEMTIME time;
  
  TCHAR process_name[32];
  DWORD process_id;
  DWORD thread_id;

  TCHAR function_name[32];
  TCHAR backtrace[BACKTRACE_BUFSIZE];

  DWORD resource_id;

  MessageType type;
  
  /* MessageType.Message */
  MessageContext context;
  DWORD domain;
  DWORD severity;
  TCHAR message[256];

  /* MessageType.Packet */
  PacketDirection direction;

  TCHAR local_address[16];
  int local_port;
  TCHAR peer_address[16];
  int peer_port;

  char buf[PACKET_BUFSIZE];
  int len;
} MessageQueueElement;

typedef void (*MessageLoggerSubmitFunc)(const MessageQueueElement *el);

void message_logger_init(MessageLoggerSubmitFunc submit_func);

void message_logger_log_full(const TCHAR *function_name, void *bt_address, DWORD resource_id, MessageType msg_type, MessageContext context, PacketDirection direction, const sockaddr_in *local_addr, const sockaddr_in *peer_addr, const char *buf, int len, const TCHAR *message, DWORD domain, DWORD severity);
void message_logger_log(const TCHAR *function_name, void *bt_address, DWORD resource_id, MessageType msg_type, MessageContext context, PacketDirection direction, const sockaddr_in *local_addr, const sockaddr_in *peer_addr, const char *buf, int len, const TCHAR *message, ...);
void message_logger_log_message(const TCHAR *function_name, void *bt_address, MessageContext context, const TCHAR *message, ...);
void message_logger_log_packet(const TCHAR *function_name, void *bt_address, DWORD resource_id, PacketDirection direction, const sockaddr_in *local_addr, const sockaddr_in *peer_addr, const char *buf, int len);

void log_tcp_listening(const TCHAR *function_name, void *bt_address, SOCKET server_socket);
void log_tcp_connecting(const TCHAR *function_name, void *bt_address, SOCKET socket, const struct sockaddr *name);
void log_tcp_connected(const TCHAR *function_name, void *bt_address, SOCKET socket, const struct sockaddr *name);
void log_tcp_client_connected(const TCHAR *function_name, void *bt_address, SOCKET server_socket, SOCKET client_socket);
void log_tcp_disconnected(const TCHAR *function_name, void *bt_address, SOCKET s, DWORD *last_error);

void log_tcp_packet(const TCHAR *function_name, void *bt_address, PacketDirection direction, SOCKET s, const char *buf, int len);
void log_udp_packet(const TCHAR *function_name, void *bt_address, PacketDirection direction, SOCKET s, const struct sockaddr *peer, const char *buf, int len);

void log_debug_w(const TCHAR *source, void *bt_address, const LPWSTR format, va_list args, DWORD domain = 0, DWORD severity = 0);
void log_debug(const TCHAR *source, void *bt_address, const char *format, va_list args, DWORD domain = 0, DWORD severity = 0);
