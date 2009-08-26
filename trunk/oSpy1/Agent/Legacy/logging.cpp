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

#pragma managed(push, off)
#include <strsafe.h>

#pragma warning( disable : 4996 )

class ResourceTracker;

static MessageLoggerSubmitFunc message_logger_submit = NULL;
static ResourceTracker *resourceTracker = NULL;

class ResourceTracker : public BaseObject
{
public:
    ResourceTracker()
    {
        InitializeCriticalSection(&cs);
    }

    ~ResourceTracker()
    {
        DeleteCriticalSection(&cs);
    }

    DWORD GetIdForSocket(SOCKET sock)
    {
        DWORD id;

        EnterCriticalSection(&cs);

        SocketToIdMap::iterator it = sockets.find(sock);
        if (it != sockets.end())
        {
            id = it->second;
        }
        else
        {
            id = ospy_rand();
            sockets[sock] = id;
        }

        LeaveCriticalSection(&cs);

        return id;
    }

    void NotifySocketDestroyed(SOCKET sock)
    {
        EnterCriticalSection(&cs);

        SocketToIdMap::iterator it = sockets.find(sock);
        if (it != sockets.end())
        {
            sockets.erase(it);
        }

        LeaveCriticalSection(&cs);
    }

private:
    typedef OMap<SOCKET, DWORD>::Type SocketToIdMap;

    CRITICAL_SECTION cs;
    SocketToIdMap sockets;
};

void
message_logger_init(MessageLoggerSubmitFunc submit_func)
{
    message_logger_submit = submit_func;

    resourceTracker = new ResourceTracker();
}

static void
message_element_init(MessageQueueElement *el,
                     const TCHAR *function_name,
                     void *bt_address,
                     DWORD resource_id,
                     MessageType msg_type)
{
    /* timestamp */
    GetLocalTime(&el->time);

    /* process name, id and thread id */
    OTString processName = CUtil::GetProcessName();
    _tcsncpy_s(el->process_name, OSPY_N_ELEMENTS(el->process_name), processName.c_str(), _TRUNCATE);
    el->process_id = GetCurrentProcessId();      
    el->thread_id = GetCurrentThreadId();

    std::string s;

    /* function name and return address */
    _tcsncpy_s(el->function_name, OSPY_N_ELEMENTS(el->function_name), function_name, _TRUNCATE);
    if (bt_address != NULL)
    {
        OTString backtrace = CUtil::CreateBackTrace(bt_address);
        _tcsncpy_s(el->backtrace, OSPY_N_ELEMENTS(el->backtrace), backtrace.c_str(), _TRUNCATE);
    }

    /* underlying resource id */
    if (resource_id == 0)
    {
        resource_id = ospy_rand();
    }

    el->resource_id = resource_id;

    /* message type */
    el->type = msg_type;
}

// FIXME: this should be dynamically allocated instead
#define LOG_BUFFER_SIZE 2048

void
message_logger_log_message(const TCHAR *function_name,
                           void *bt_address,
                           MessageContext context,
                           const TCHAR *message,
                           ...)
{
    va_list args;
    TCHAR buf[LOG_BUFFER_SIZE];

    va_start(args, message);
    StringCbVPrintf(buf, sizeof(buf), message, args);

    message_logger_log_full(function_name, bt_address, 0,
        MESSAGE_TYPE_MESSAGE, context, PACKET_DIRECTION_INVALID,
        NULL, NULL, NULL, 0, buf);
}

void
message_logger_log_packet(const TCHAR *function_name,
                          void *bt_address,
                          DWORD resource_id,
                          PacketDirection direction,
                          const sockaddr_in *local_addr,
                          const sockaddr_in *peer_addr,
                          const char *buf,
                          int len)
{
    message_logger_log(function_name, bt_address, resource_id,
                       MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, direction,
                       local_addr, peer_addr, buf, len, NULL);
}

void
message_logger_log_full(const TCHAR *function_name,
                        void *bt_address,
                        DWORD resource_id,
                        MessageType msg_type,
                        MessageContext context,
                        PacketDirection direction,
                        const sockaddr_in *local_addr,
                        const sockaddr_in *peer_addr,
                        const char *buf,
                        int len,
                        const TCHAR *message)
{
    MessageQueueElement el;
    int read_len;

    memset(&el, 0, sizeof(MessageQueueElement));

    /* fill in basic fields */
    message_element_init(&el, function_name, bt_address, resource_id, msg_type);

    /* context */
    el.context = context;

    /* direction */
    el.direction = direction;

    /* fill in local address and port */
    if (local_addr)
    {
        memcpy(&el.local_address, local_addr, sizeof(sockaddr_in));
    }

    /* fill in peer address and port */
    if (peer_addr)
    {
        memcpy(&el.peer_address, peer_addr, sizeof(sockaddr_in));
    }

    /* copy the buffer */
    if (len > PACKET_BUFSIZE)
    {
        read_len = PACKET_BUFSIZE;
        message_logger_log_message(_T("message_logger_log_packet"), 0,
            MESSAGE_CTX_WARNING, _T("packet read clamped to %d bytes, needed %d"),
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

    if (message != NULL)
    {
        _tcsncpy_s(el.message, OSPY_N_ELEMENTS(el.message), message, _TRUNCATE);
        el.message[sizeof(el.message) - 1] = '\0';
    }

    /* submit the message */
    message_logger_submit(&el);
}

void
message_logger_log(const TCHAR *function_name,
                   void *bt_address,
                   DWORD resource_id,
                   MessageType msg_type,
                   MessageContext context,
                   PacketDirection direction,
                   const sockaddr_in *local_addr,
                   const sockaddr_in *peer_addr,
                   const char *buf,
                   int len,
                   const TCHAR *message,
                   ...)
{
    va_list args;
    TCHAR msg_buf[LOG_BUFFER_SIZE];

    if (message != NULL)
    {
        va_start(args, message);
        StringCbVPrintf(msg_buf, sizeof(msg_buf), message, args);

        message = msg_buf;
    }

    message_logger_log_full(function_name, bt_address, resource_id,
        msg_type, context, direction, local_addr, peer_addr, buf, len,
        message);
}


/****************************************************************************
 * Logging utility functions                                                *
 ****************************************************************************/

class SocketAddress
{
public:
    SocketAddress(const struct sockaddr *addr)
        : Ipv4Address(NULL)
    {
        OTStringStream ss;

        if (addr != NULL)
        {
            if (addr->sa_family == AF_INET)
            {
                Ipv4Address = reinterpret_cast<const struct sockaddr_in *>(addr);

                TCHAR buf[16];
                CUtil::Ipv4AddressToString(&Ipv4Address->sin_addr, buf);
                ss << buf << _T(":") << static_cast<DWORD>(ntohs(Ipv4Address->sin_port));
            }
            else
            {
                ss << _T("<unhandled address family 0x") << hex << addr->sa_family << _T(">");
            }
        }
        else
        {
            ss << _T("<null>");
        }

        descStr = ss.str();
        Description = descStr.c_str();
    }

    const TCHAR *Description;
    const struct sockaddr_in *Ipv4Address;

private:
    OTString descStr;
};

class Socket
{
public:
    Socket(SOCKET sock)
        : LocalIpv4Address(NULL), PeerIpv4Address(NULL)
    {
        int len;
        
        len = sizeof(localAddr);
        if (getsockname(sock, &localAddr, &len) == 0)
        {
            SocketAddress sa(&localAddr);
            localDescStr = sa.Description;

            if (localAddr.sa_family == AF_INET)
                LocalIpv4Address = reinterpret_cast<struct sockaddr_in *>(&localAddr);
        }
        else
        {
            localDescStr = _T("<unbound>");
        }

        len = sizeof(peerAddr);
        if (getpeername(sock, &peerAddr, &len) == 0)
        {
            SocketAddress sa(&peerAddr);
            peerDescStr = sa.Description;

            if (peerAddr.sa_family == AF_INET)
                PeerIpv4Address = reinterpret_cast<struct sockaddr_in *>(&peerAddr);
        }
        else
        {
            peerDescStr = _T("<unconnected>");
        }

        LocalDescription = localDescStr.c_str();
        PeerDescription = peerDescStr.c_str();
    }

    const TCHAR *LocalDescription;
    const struct sockaddr_in *LocalIpv4Address;

    const TCHAR *PeerDescription;
    const struct sockaddr_in *PeerIpv4Address;

private:
    OTString localDescStr;
    struct sockaddr localAddr;

    OTString peerDescStr;
    struct sockaddr peerAddr;
};

void
log_tcp_listening(const TCHAR *function_name,
                  void *bt_address,
                  SOCKET server_socket)
{
    Socket sock(server_socket);
    message_logger_log(function_name, bt_address, resourceTracker->GetIdForSocket(server_socket),
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_LISTENING, PACKET_DIRECTION_INCOMING,
                       sock.LocalIpv4Address, NULL, NULL, 0,
                       _T("%s: listening for connections"), sock.LocalDescription);
}

void
log_tcp_connecting(const TCHAR *function_name,
                   void *bt_address,
                   SOCKET socket,
                   const struct sockaddr *name)
{
    Socket sock(socket);
    SocketAddress remoteAddr(name);
    message_logger_log(function_name, bt_address, resourceTracker->GetIdForSocket(socket),
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_CONNECTING, PACKET_DIRECTION_OUTGOING,
                       sock.LocalIpv4Address, remoteAddr.Ipv4Address, NULL, 0,
                       _T("%s: connecting to %s"), sock.LocalDescription, remoteAddr.Description);
}

void
log_tcp_connected(const TCHAR *function_name,
                  void *bt_address,
                  SOCKET socket,
                  const struct sockaddr *name)
{
    Socket sock(socket);
    SocketAddress remoteAddr(name);
    message_logger_log(function_name, bt_address, resourceTracker->GetIdForSocket(socket),
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_CONNECTED, PACKET_DIRECTION_OUTGOING,
                       sock.LocalIpv4Address, remoteAddr.Ipv4Address, NULL, 0,
                       _T("%s: connected to %s"), sock.LocalDescription, remoteAddr.Description);
}

void
log_tcp_client_connected(const TCHAR *function_name,
                         void *bt_address,
                         SOCKET server_socket,
                         SOCKET client_socket)
{
    Socket servSock(server_socket);
    Socket clientSock(client_socket);
    message_logger_log(function_name, bt_address,
                       resourceTracker->GetIdForSocket(client_socket),
                       MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_SOCKET_CONNECTED,
                       PACKET_DIRECTION_INCOMING, servSock.LocalIpv4Address, clientSock.PeerIpv4Address, NULL, 0,
                       _T("%s: client connected from %s"), servSock.LocalDescription, clientSock.PeerDescription);
}

void
log_tcp_disconnected(const TCHAR *function_name,
                     void *bt_address,
                     SOCKET s,
                     DWORD *last_error)
{
    Socket sock(s);
    message_logger_log(function_name, bt_address, resourceTracker->GetIdForSocket(s), MESSAGE_TYPE_MESSAGE,
                       (last_error == NULL) ? MESSAGE_CTX_SOCKET_DISCONNECTED : MESSAGE_CTX_SOCKET_RESET,
                       PACKET_DIRECTION_INVALID, sock.LocalIpv4Address, sock.PeerIpv4Address, (const char *) last_error,
                       (last_error == NULL) ? 0 : 4,
                       _T("%s: connection to %s %s"), sock.LocalDescription, sock.PeerDescription,
                       (last_error == NULL) ? _T("closed") : _T("reset"));
}

void
log_tcp_packet(const TCHAR *function_name,
               void *bt_address,
               PacketDirection direction,
               SOCKET s, const char *buf,
               int len)
{
    Socket sock(s);
    message_logger_log_packet(function_name, bt_address, resourceTracker->GetIdForSocket(s), direction,
                              sock.LocalIpv4Address, sock.PeerIpv4Address, buf, len);
}

void
log_udp_packet(const TCHAR *function_name,
               void *bt_address,
               PacketDirection direction,
               SOCKET s,
               const struct sockaddr *peer,
               const char *buf,
               int len)
{
    Socket sock(s);
    SocketAddress peerAddr(peer);
    message_logger_log_packet(function_name, bt_address, resourceTracker->GetIdForSocket(s), direction,
                              sock.LocalIpv4Address, (peer != NULL) ? peerAddr.Ipv4Address : sock.PeerIpv4Address,
                              buf, len);
}

void
log_socket_closed(void *bt_address,
                  SOCKET s)
{
    Socket sock(s);

    if (sock.LocalIpv4Address != NULL)
    {
        message_logger_log(_T("closesocket"), bt_address, resourceTracker->GetIdForSocket(s), MESSAGE_TYPE_MESSAGE,
                           MESSAGE_CTX_SOCKET_DISCONNECTED, PACKET_DIRECTION_INVALID,
                           sock.LocalIpv4Address, sock.PeerIpv4Address, NULL, 0,
                           _T("%s: closed"), sock.LocalDescription);
    }
    else
    {
        message_logger_log(_T("closesocket"), bt_address, resourceTracker->GetIdForSocket(s), MESSAGE_TYPE_MESSAGE,
                           MESSAGE_CTX_SOCKET_DISCONNECTED, PACKET_DIRECTION_INVALID,
                           sock.LocalIpv4Address, sock.PeerIpv4Address, NULL, 0,
                           _T(""));
    }

    resourceTracker->NotifySocketDestroyed(s);
}

void
log_debug_w(const TCHAR *source,
            void *bt_address,
            const LPWSTR format,
            va_list args,
            DWORD domain,
            DWORD severity)
{
    WCHAR wide_buf[LOG_BUFFER_SIZE];
    char buf[LOG_BUFFER_SIZE];

    StringCbVPrintfW(wide_buf, sizeof(wide_buf), format, args);

    WideCharToMultiByte(CP_ACP, 0, wide_buf, -1, buf, sizeof(buf), NULL, NULL);

    message_logger_log_full(source, bt_address, 0, MESSAGE_TYPE_MESSAGE,
        MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID, NULL, NULL,
        buf, strlen(buf), wide_buf);
}

void
log_debug(const TCHAR *source,
          void *bt_address,
          const char *format,
          va_list args,
          DWORD domain,
          DWORD severity)
{
    char buf[LOG_BUFFER_SIZE];
    WCHAR wide_buf[LOG_BUFFER_SIZE];

    StringCbVPrintfA(buf, sizeof(buf), format, args);

    MultiByteToWideChar(CP_ACP, 0, buf, -1, wide_buf, LOG_BUFFER_SIZE);

    message_logger_log_full(source, bt_address, 0, MESSAGE_TYPE_MESSAGE,
        MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID, NULL, NULL, NULL, 0, wide_buf);
}

#pragma managed(pop)
