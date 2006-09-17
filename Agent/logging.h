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
  
  char process_name[32];
  DWORD process_id;
  DWORD thread_id;

  char function_name[16];
  DWORD return_address;
  char caller_module_name[32];

  DWORD resource_id;

  MessageType type;
  
  /* MessageType.Message */
  MessageContext context;
  char message[256];

  /* MessageType.Packet */
  PacketDirection direction;

  char local_address[16];
  int local_port;
  char peer_address[16];
  int peer_port;

  char buf[PACKET_BUFSIZE];
  int len;
} MessageQueueElement;

typedef struct {
    int num_softwall_rules;
    SoftwallRule rules[MAX_SOFTWALL_RULES];

    int num_elements;
    MessageQueueElement elements[MAX_ELEMENTS];
} MessageQueue;

void message_logger_init();
void message_logger_get_queue(MessageQueue **queue, HANDLE *queue_mutex);

void message_logger_log(const char *function_name, DWORD return_address, DWORD resource_id, MessageType msg_type, MessageContext context, PacketDirection direction, const sockaddr_in *local_addr, const sockaddr_in *peer_addr, const char *buf, int len, const char *message, ...);
void message_logger_log_message(const char *function_name, DWORD return_address, MessageContext context, const char *message, ...);
void message_logger_log_packet(const char *function_name, DWORD return_address, DWORD resource_id, PacketDirection direction, const sockaddr_in *local_addr, const sockaddr_in *peer_addr, const char *buf, int len);

void log_tcp_listening(const char *function_name, DWORD return_address, SOCKET server_socket);
void log_tcp_connecting(const char *function_name, DWORD return_address, SOCKET socket, const struct sockaddr *name);
void log_tcp_connected(const char *function_name, DWORD return_address, SOCKET socket, const struct sockaddr *name);
void log_tcp_client_connected(const char *function_name, DWORD return_address, SOCKET server_socket, SOCKET client_socket);
void log_tcp_disconnected(const char *function_name, DWORD return_address, SOCKET s, DWORD *last_error);

void log_tcp_packet(const char *function_name, DWORD return_address, PacketDirection direction, SOCKET s, const char *buf, int len);
void log_udp_packet(const char *function_name, DWORD return_address, PacketDirection direction, SOCKET s, const struct sockaddr *peer, const char *buf, int len);

void log_debug_w(const char *source, DWORD ret_addr, const LPWSTR format, va_list args);
void log_debug(const char *source, DWORD ret_addr, const char *format, va_list args);