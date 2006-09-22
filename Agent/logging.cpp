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

#include "stdafx.h"
#include "logging.h"
#include "util.h"
#include <stdio.h>
#include <strsafe.h>

#pragma warning( disable : 4996 )

#define LOG_FILENAME "C:\\oSpyAgent_log.txt"

MessageQueue *queue = NULL;
HANDLE queue_mutex = INVALID_HANDLE_VALUE;

static HANDLE ready_event = INVALID_HANDLE_VALUE;

void
message_logger_init()
{
    HANDLE map;
    BOOL exists;

    map = CreateFileMapping(INVALID_HANDLE_VALUE, NULL,
                            PAGE_READWRITE, 0, sizeof(MessageQueue),
                            "BadgerPacketQueue");
    exists = (GetLastError() == ERROR_ALREADY_EXISTS);

    if (exists)
    {
        map = OpenFileMapping(FILE_MAP_WRITE, FALSE, "BadgerPacketQueue");
    }

    queue = (MessageQueue *) MapViewOfFile(map, FILE_MAP_WRITE, 0, 0, sizeof(MessageQueue));

    if (!exists)
    {
        queue->num_softwall_rules = 0;
        queue->num_elements = 0;
    }

    queue_mutex = CreateMutex(NULL, FALSE, "BadgerQueueMutex");

    ready_event = CreateEvent(NULL,
                              FALSE, /* bManualReset */
                              FALSE, /* bInitialState */
                              "BadgerPacketReady");
}

void
message_logger_get_queue(MessageQueue **ret_queue, HANDLE *ret_queue_mutex)
{
    *ret_queue = queue;
    *ret_queue_mutex = queue_mutex;
}

static void
message_element_init(MessageQueueElement *el,
                     const char *function_name,
                     DWORD return_address,
                     DWORD resource_id,
                     MessageType msg_type)
{
    /* timestamp */
    GetLocalTime(&el->time);

    /* process name, id and thread id */
    get_process_name(el->process_name, sizeof(el->process_name));
    el->process_id = GetCurrentProcessId();      
    el->thread_id = GetCurrentThreadId();

    /* function name and return address */
    strcpy(el->function_name, function_name);
    el->return_address = return_address;

    get_module_name_for_address((LPVOID) return_address,
                              el->caller_module_name,
                              sizeof(el->caller_module_name));

    /* underlying resource id */
    el->resource_id = resource_id;

    /* message type */
    el->type = msg_type;
}

static
void message_queue_add(MessageQueueElement *element)
{
    if (WaitForSingleObject(queue_mutex, INFINITE) == WAIT_OBJECT_0)
    {
        __try
        {
            if (queue->num_elements == MAX_ELEMENTS)
                return;

            queue->elements[queue->num_elements++] = *element;
        }
        __finally
        {
            ReleaseMutex(queue_mutex);
        }

        /* notify logger thread */
        SetEvent(ready_event);
    }
}

void
message_logger_log_message(const char *function_name,
                           DWORD return_address,
                           MessageContext context,
                           const char *message,
                           ...)
{
    va_list args;
    char buf[256];

    va_start(args, message);
    vsprintf(buf, message, args);

    message_logger_log(function_name, return_address, 0,
                       MESSAGE_TYPE_MESSAGE, context, PACKET_DIRECTION_INVALID,
                       NULL, NULL, NULL, 0, buf);
}

void
message_logger_log_packet(const char *function_name,
                          DWORD return_address,
                          DWORD resource_id,
                          PacketDirection direction,
                          const sockaddr_in *local_addr,
                          const sockaddr_in *peer_addr,
                          const char *buf,
                          int len)
{
    message_logger_log(function_name, return_address, resource_id,
                       MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, direction,
                       local_addr, peer_addr, buf, len, NULL);
}

void
message_logger_log(const char *function_name,
                   DWORD return_address,
                   DWORD resource_id,
                   MessageType msg_type,
                   MessageContext context,
                   PacketDirection direction,
                   const sockaddr_in *local_addr,
                   const sockaddr_in *peer_addr,
                   const char *buf,
                   int len,
                   const char *message,
                   ...)
{
    MessageQueueElement el;
    int read_len;
    va_list args;

    va_start(args, message);

    memset(&el, 0, sizeof(MessageQueueElement));

    /* fill in basic fields */
    message_element_init(&el, function_name, return_address, resource_id, msg_type);

    /* context */
    el.context = context;

    /* direction */
    el.direction = direction;

    /* fill in local address and port */
    if (local_addr)
    {
        strcpy(el.local_address, inet_ntoa(local_addr->sin_addr));
        el.local_port = ntohs(local_addr->sin_port);
    }

    /* fill in peer address and port */
    if (peer_addr)
    {
        strcpy(el.peer_address, inet_ntoa(peer_addr->sin_addr));
        el.peer_port = ntohs(peer_addr->sin_port);
    }

    /* copy the buffer */
    if (len > PACKET_BUFSIZE)
    {
        read_len = PACKET_BUFSIZE;
        message_logger_log_message("message_logger_log_packet", 0,
          MESSAGE_CTX_WARNING, "packet read clamped to %d bytes, needed %d",
          read_len, len);
    }
    else
    {
        read_len = len;
    }

    if (buf != NULL)
    {
        memcpy(el.buf, buf, read_len);
        el.len = read_len;
    }

    /* fill in the message content */
    if (message != NULL)
    {
        vsprintf(el.message, message, args);
    }

    /* submit the message */
    message_queue_add(&el);
}

/****************************************************************************
 * Logging utility functions                                                *
 ****************************************************************************/

void
log_tcp_listening(const char *function_name,
                  DWORD return_address,
                  SOCKET server_socket)
{
	struct sockaddr_in sin;
	int sin_len;

    /* local address */
    sin_len = sizeof(sin);
    getsockname(server_socket, (struct sockaddr *) &sin, &sin_len);

    message_logger_log(function_name, return_address, server_socket,
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_LISTENING,
                       PACKET_DIRECTION_INCOMING, &sin, NULL, NULL, 0,
                       "%s:%d: listening for connections",
                       inet_ntoa(sin.sin_addr), ntohs(sin.sin_port));
}

void
log_tcp_connecting(const char *function_name,
                   DWORD return_address,
                   SOCKET socket,
                   const struct sockaddr *name)
{
	struct sockaddr_in sin;
	int sin_len;
    const sockaddr_in *peer_addr = (const sockaddr_in *) name;
    char local_addr_str[16], peer_addr_str[16];

    /* local address */
    sin_len = sizeof(sin);
    getsockname(socket, (struct sockaddr *) &sin, &sin_len);

    strcpy(local_addr_str, inet_ntoa(sin.sin_addr));
    strcpy(peer_addr_str, inet_ntoa(peer_addr->sin_addr));

    message_logger_log(function_name, return_address, socket,
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_CONNECTING,
                       PACKET_DIRECTION_OUTGOING, &sin, peer_addr, NULL, 0,
                       "%s:%d: connecting to %s:%d",
                       local_addr_str, ntohs(sin.sin_port),
                       peer_addr_str, ntohs(peer_addr->sin_port));
}

void
log_tcp_connected(const char *function_name,
                  DWORD return_address,
                  SOCKET socket,
                  const struct sockaddr *name)
{
	struct sockaddr_in sin;
	int sin_len;
    const sockaddr_in *peer_addr = (const sockaddr_in *) name;
    char local_addr_str[16], peer_addr_str[16];

    /* local address */
    sin_len = sizeof(sin);
    getsockname(socket, (struct sockaddr *) &sin, &sin_len);

    strcpy(local_addr_str, inet_ntoa(sin.sin_addr));
    strcpy(peer_addr_str, inet_ntoa(peer_addr->sin_addr));

    message_logger_log(function_name, return_address, socket,
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_CONNECTED,
                       PACKET_DIRECTION_OUTGOING, &sin, peer_addr, NULL, 0,
                       "%s:%d: connected to %s:%d",
                       local_addr_str, ntohs(sin.sin_port),
                       peer_addr_str, ntohs(peer_addr->sin_port));
}

void
log_tcp_client_connected(const char *function_name,
                         DWORD return_address,
                         SOCKET server_socket,
                         SOCKET client_socket)
{
    struct sockaddr_in sin_local, sin_peer;
    int sin_len;
    char local_addr_str[16], peer_addr_str[16];

    /* local address
     *
     * FIXME: maybe client_socket should be used instead?
     */
    sin_len = sizeof(sin_local);
    getsockname(server_socket, (struct sockaddr *) &sin_local, &sin_len);
    strcpy(local_addr_str, inet_ntoa(sin_local.sin_addr));

    /* peer address */
    sin_len = sizeof(sin_peer);
    getpeername(client_socket, (struct sockaddr *) &sin_peer, &sin_len);
    strcpy(peer_addr_str, inet_ntoa(sin_peer.sin_addr));

    message_logger_log(function_name, return_address, client_socket,
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_CONNECTED,
                       PACKET_DIRECTION_INCOMING, &sin_local, &sin_peer, NULL,
                       0, "%s:%d: client connected from %s:%d",
                       local_addr_str, ntohs(sin_local.sin_port),
                       peer_addr_str, ntohs(sin_peer.sin_port));
}

void
log_tcp_disconnected(const char *function_name,
                     DWORD return_address,
                     SOCKET s,
                     DWORD *last_error)
{
    struct sockaddr_in sin_local, sin_peer;
    int sin_len;
    char local_addr_str[16], peer_addr_str[16];

    /* local address */
    sin_len = sizeof(sin_local);
    getsockname(s, (struct sockaddr *) &sin_local, &sin_len);
    strcpy(local_addr_str, inet_ntoa(sin_local.sin_addr));

    /* peer address */
    sin_len = sizeof(sin_peer);
    getpeername(s, (struct sockaddr *) &sin_peer, &sin_len);
    strcpy(peer_addr_str, inet_ntoa(sin_peer.sin_addr));

    message_logger_log(function_name, return_address, s, MESSAGE_TYPE_MESSAGE,
                       (last_error == NULL) ? MESSAGE_CTX_SOCKET_DISCONNECTED : MESSAGE_CTX_SOCKET_RESET,
                       PACKET_DIRECTION_INVALID, &sin_local, &sin_peer, (const char *) last_error,
                       (last_error == NULL) ? 0 : 4, "%s:%d: connection to %s:%d %s",
                       local_addr_str, ntohs(sin_local.sin_port),
                       peer_addr_str, ntohs(sin_peer.sin_port),
                       (last_error == NULL) ? "closed" : "reset");
}

void
log_tcp_packet(const char *function_name,
               DWORD return_address,
               PacketDirection direction,
               SOCKET s, const char *buf,
               int len)
{
    struct sockaddr_in local_addr, peer_addr;
    int sin_len;

    sin_len = sizeof(local_addr);
    getsockname(s, (struct sockaddr *) &local_addr, &sin_len);

    sin_len = sizeof(peer_addr);
    getpeername(s, (struct sockaddr *) &peer_addr, &sin_len);

    message_logger_log_packet(function_name, return_address, s, direction,
                              &local_addr, &peer_addr, buf, len);
}

void
log_udp_packet(const char *function_name,
               DWORD return_address,
               PacketDirection direction,
               SOCKET s,
               const struct sockaddr *peer,
               const char *buf,
               int len)
{
	struct sockaddr_in local_addr, peer_addr;
	int sin_len;

  sin_len = sizeof(local_addr);
  getsockname(s, (struct sockaddr *) &local_addr, &sin_len);

  if (peer == NULL)
  {
    sin_len = sizeof(peer_addr);
    getsockname(s, (struct sockaddr *) &peer_addr, &sin_len);    
  }
  else
  {
    peer_addr = *((const struct sockaddr_in *) peer);
  }

  message_logger_log_packet(function_name, return_address, s, direction,
                            &local_addr, &peer_addr, buf, len);
}

// FIXME: this should be dynamically allocated instead
#define LOG_BUFFER_SIZE 2048

void
log_debug_w(const char *source,
            DWORD ret_addr,
            const LPWSTR format,
            va_list args)
{
    WCHAR wide_buf[LOG_BUFFER_SIZE];
    char buf[LOG_BUFFER_SIZE];

    StringCbVPrintfW(wide_buf, sizeof(wide_buf), format, args);
    wide_buf[LOG_BUFFER_SIZE - 1] = L'\0';

    WideCharToMultiByte(CP_ACP, 0, wide_buf, -1, buf, sizeof(buf), NULL, NULL);

    message_logger_log_message(source, ret_addr, MESSAGE_CTX_INFO, buf);
}

void
log_debug(const char *source,
          DWORD ret_addr,
          const char *format,
          va_list args)
{
    char buf[256];

    vsprintf(buf, format, args);

    message_logger_log_message(source, ret_addr, MESSAGE_CTX_INFO, buf);
}
