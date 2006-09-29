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
#include "hooking.h"
#include "logging.h"
#include <Ws2tcpip.h>
#include <psapi.h>

static MODULEINFO wsock32_info;

static int __cdecl
getaddrinfo_called(BOOL carry_on,
                   DWORD ret_addr,
                   const char *nodename,
                   const char *servname,
                   const struct addrinfo *hints,
                   struct addrinfo **res)
{
    return 0;
}

static void
dump_addrinfo(const struct addrinfo *first_ai,
              ByteBuffer *buf)
{
    const struct addrinfo *el;

    for (el = first_ai; el; el = el->ai_next)
    {
        /*
         * ai_flags
         */
        byte_buffer_append_printf(buf, "\r\n  <flags=");

        if (el->ai_flags != 0)
        {
            bool empty = true;

            if (el->ai_flags & AI_PASSIVE)
            {
                empty = false;
                byte_buffer_append_printf(buf, "PASSIVE");
            }

            if (el->ai_flags & AI_CANONNAME)
            {
                if (!empty)  byte_buffer_append_printf(buf, "|");
                empty = false;
                byte_buffer_append_printf(buf, "CANONNAME");
            }

            if (el->ai_flags & AI_NUMERICHOST)
            {
                if (!empty)  byte_buffer_append_printf(buf, "|");
                byte_buffer_append_printf(buf, "NUMERICHOST");
            }
        }
        else
        {
            byte_buffer_append_printf(buf, "0");
        }

        /*
         * ai_family
         */
        byte_buffer_append_printf(buf, ", family=");

        switch (el->ai_family)
        {
            case PF_INET:
                byte_buffer_append_printf(buf, "INET");
                break;
            case PF_INET6:
                byte_buffer_append_printf(buf, "INET6");
                break;
            default:
                byte_buffer_append_printf(buf,
                    (el->ai_family != 0) ? "0x%08x" : "%d", el->ai_family);
                break;
        }

        /*
         * ai_socktype
         */
        byte_buffer_append_printf(buf, ", socktype=");

        switch (el->ai_socktype)
        {
            case SOCK_STREAM:
                byte_buffer_append_printf(buf, "STREAM");
                break;
            case SOCK_DGRAM:
                byte_buffer_append_printf(buf, "DGRAM");
                break;
            case SOCK_RAW:
                byte_buffer_append_printf(buf, "RAW");
                break;
            default:
                byte_buffer_append_printf(buf,
                    (el->ai_socktype != 0) ? "0x%08x" : "%d", el->ai_socktype);
                break;
        }

        /*
         * ai_protocol
         */
        byte_buffer_append_printf(buf, ", protocol=");

        switch (el->ai_protocol)
        {
            case IPPROTO_TCP:
                byte_buffer_append_printf(buf, "TCP");
                break;
            case IPPROTO_UDP:
                byte_buffer_append_printf(buf, "UDP");
                break;
            default:
                byte_buffer_append_printf(buf,
                    (el->ai_protocol != 0) ? "0x%08x" : "%d", el->ai_protocol);
                break;
        }

        /*
         * ai_canonname
         */
        byte_buffer_append_printf(buf, ", canonname=");

        if (el->ai_canonname != NULL)
        {
            byte_buffer_append_printf(buf, "\"%s\"", el->ai_canonname);
        }
        else
        {
            byte_buffer_append_printf(buf, "NULL");
        }

        /*
         * ai_addr
         */
        byte_buffer_append_printf(buf, ", addr=");

        if (el->ai_addr != NULL)
        {
            struct sockaddr_in *sin;
            int port;

            switch (el->ai_family)
            {
                case PF_INET:
                    sin = (struct sockaddr_in *) el->ai_addr;

                    byte_buffer_append_printf(buf, "%s", inet_ntoa(sin->sin_addr));

                    port = ntohs(sin->sin_port);
                    if (port != 0)
                    {
                        byte_buffer_append_printf(buf, ":%d", port);
                    }

                    break;
                default:
                    byte_buffer_append_printf(buf, "?");
                    break;
            }
        }
        else
        {
            byte_buffer_append_printf(buf, "NULL");
        }

        byte_buffer_append_printf(buf, ">");
    }
}

static int __stdcall
getaddrinfo_done(int retval,
                 const char *nodename,
                 const char *servname,
                 const struct addrinfo *hints,
                 struct addrinfo **res)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));
    ByteBuffer *msg = byte_buffer_sized_new(64);
    ByteBuffer *body = byte_buffer_sized_new(256);
    const char *nn, *sn;

    nn = (nodename != NULL) ? nodename : "NULL";
    sn = (servname != NULL) ? servname : "NULL";

    byte_buffer_append_printf(msg, "nodename=%s, servname=%s", nn, sn);

    byte_buffer_append_printf(body, "nodename: %s\r\nservname: %s\r\nhints:",
        nn, sn);

    dump_addrinfo(hints, body);

    byte_buffer_append_printf(body, "\r\nresult:");

    if (retval == 0)
    {
        dump_addrinfo(*res, body);
    }
    else
    {
        char *str = NULL;

        switch (err)
        {
            case EAI_AGAIN:    str = "EAI_AGAIN";    break;
            case EAI_BADFLAGS: str = "EAI_BADFLAGS"; break;
            case EAI_FAIL:     str = "EAI_FAIL";     break;
            case EAI_FAMILY:   str = "EAI_FAMILY";   break;
            case EAI_MEMORY:   str = "EAI_MEMORY";   break;
            //case EAI_NODATA:   str = "EAI_NODATA";   break;
            case EAI_NONAME:   str = "EAI_NONAME";   break;
            case EAI_SERVICE:  str = "EAI_SERVICE";  break;
            case EAI_SOCKTYPE: str = "EAI_SOCKTYPE"; break;
            default:                                 break;
        }

        byte_buffer_append_printf(body, "\r\n  ");

        if (str != NULL)
        {
            byte_buffer_append_printf(body, "%s", str);
        }
        else
        {
            byte_buffer_append_printf(body, "ERROR_0x%08x", err);
        }
    }

    message_logger_log("getaddrinfo", ret_addr, 0, MESSAGE_TYPE_PACKET,
        MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID, NULL, NULL,
        (const char *) body->buf, (int) body->offset,
        (const char *) msg->buf);

    byte_buffer_free(msg);
    byte_buffer_free(body);

    SetLastError(err);
    return retval;
}

static int __cdecl
closesocket_called(BOOL carry_on,
                   DWORD ret_addr,
                   SOCKET s)
{
    log_tcp_disconnected("closesocket", ret_addr, s, NULL);
    return 0;
}

static int __stdcall
closesocket_done(int retval,
                 SOCKET s)
{
    return retval;
}

static int __cdecl
recv_called(BOOL carry_on,
            DWORD ret_addr,
            SOCKET s,
            char *buf,
            int len,
            int flags)
{
    return softwall_decide_from_socket("recv", ret_addr, s, &carry_on);
}

static int __stdcall
recv_done(int retval,
          SOCKET s,
          char *buf,
          int len,
          int flags)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval > 0)
    {
        log_tcp_packet("recv", ret_addr, PACKET_DIRECTION_INCOMING, s, buf, retval);
    }
    else if (retval == 0)
    {
        log_tcp_disconnected("recv", ret_addr, s, NULL);
    }
    else if (retval == SOCKET_ERROR)
    {
        if (err != WSAEWOULDBLOCK)
        {
            log_tcp_disconnected("recv", ret_addr, s, &err);
        }
    }

    SetLastError(err);
    return retval;
}

static int __cdecl
send_called(BOOL carry_on,
            DWORD ret_addr,
            SOCKET s,
            const char *buf,
            int len,
            int flags)
{
    return softwall_decide_from_socket("send", ret_addr, s, &carry_on);
}

static int __stdcall
send_done(int retval,
          SOCKET s,
          const char *buf,
          int len,
          int flags)
{
  DWORD err = GetLastError();
  int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

  if (retval > 0)
  {
    log_tcp_packet("send", ret_addr, PACKET_DIRECTION_OUTGOING, s, buf, retval);
  }
  else if (retval == SOCKET_ERROR)
  {
    if (err != WSAEWOULDBLOCK)
    {
      log_tcp_disconnected("send", ret_addr, s, &err);
    }
  }

  SetLastError(err);
  return retval;
}

static int __cdecl
recvfrom_called(BOOL carry_on,
                DWORD ret_addr,
                SOCKET s,
                char *buf,
                int len,
                int flags,
                struct sockaddr *from,
                int *fromlen)
{
    return 0;
}

static int __stdcall
recvfrom_done(int retval,
              SOCKET s,
              char *buf,
              int len,
              int flags,
              struct sockaddr *from,
              int *fromlen)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));
    int overridden_retval;
    BOOL carry_on;

    overridden_retval =
        softwall_decide_from_socket_and_remote_address("recvfrom", ret_addr,
                                                       s, (const sockaddr_in *) from,
                                                       &carry_on);

    if (!carry_on)
        return overridden_retval;

    if (retval > 0)
        log_udp_packet("recvfrom", ret_addr, PACKET_DIRECTION_INCOMING, s, from, buf, retval);

    SetLastError(err);
    return retval;
}

static int __cdecl
sendto_called(BOOL carry_on,
              DWORD ret_addr,
              SOCKET s,
              const char *buf,
              int len,
              int flags,
              const struct sockaddr *to,
              int tolen)
{
    return 0;
}

static int __stdcall
sendto_done(int retval,
            SOCKET s,
            const char* buf,
            int len,
            int flags,
            const struct sockaddr *to,
            int tolen)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));
    int overridden_retval;
    BOOL carry_on;

    overridden_retval =
        softwall_decide_from_socket_and_remote_address("sendto", ret_addr,
                                                       s, (const sockaddr_in *) to,
                                                       &carry_on);

    if (!carry_on)
        return overridden_retval;

    if (retval > 0)
        log_udp_packet("sendto", ret_addr, PACKET_DIRECTION_OUTGOING, s, to, buf, retval);

    SetLastError(err);
    return retval;
}

static SOCKET __cdecl
accept_called(BOOL carry_on,
              DWORD ret_addr,
              SOCKET s,
              struct sockaddr *addr,
              int *addrlen)
{
    int retval =
        softwall_decide_from_socket_and_remote_address("accept", ret_addr, s, NULL, &carry_on);

    if (carry_on)
        log_tcp_listening("accept", ret_addr, s);

    return retval;
}

static SOCKET __stdcall
accept_done(SOCKET retval,
            SOCKET s,
            struct sockaddr *addr,
            int *addrlen)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));
    int overridden_retval;
    BOOL carry_on;

    overridden_retval =
        softwall_decide_from_socket_and_remote_address("accept", ret_addr, s,
                                                       (const sockaddr_in *) addr,
                                                       &carry_on);

    if (!carry_on)
        return overridden_retval;

    if (retval != INVALID_SOCKET)
        log_tcp_client_connected("accept", ret_addr, s, retval);

    SetLastError(err);
    return retval;
}

static int __cdecl
connect_called(BOOL carry_on,
               DWORD ret_addr,
               SOCKET s,
               const struct sockaddr *name,
               int namelen)
{
    int retval =
        softwall_decide_from_socket_and_remote_address("connect", ret_addr, s,
                                                       (const sockaddr_in *) name,
                                                       &carry_on);

    if (carry_on)
        log_tcp_connecting("connect", ret_addr, s, name);

    return retval;
}

static int __stdcall
connect_done(int retval,
             SOCKET s,
             const struct sockaddr *name,
             int namelen)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval != SOCKET_ERROR)
    {
        log_tcp_connected("connect", ret_addr, s, name);
    }

    SetLastError(err);
    return retval;
}

static BOOL
called_from_wsock(DWORD ret_addr)
{
    return (ret_addr >= (DWORD) wsock32_info.lpBaseOfDll &&
            ret_addr < (DWORD) wsock32_info.lpBaseOfDll + wsock32_info.SizeOfImage);
}

int __cdecl
WSARecv_called(BOOL carry_on,
               DWORD ret_addr,
               SOCKET s,
               LPWSABUF lpBuffers,
               DWORD dwBufferCount,
               LPDWORD lpNumberOfBytesRecvd,
               LPDWORD lpFlags,
               LPWSAOVERLAPPED lpOverlapped,
               LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
    return softwall_decide_from_socket("WSARecv", ret_addr, s, &carry_on);
}

static int __stdcall
WSARecv_done(int retval,
             SOCKET s,
             LPWSABUF lpBuffers,
             DWORD dwBufferCount,
             LPDWORD lpNumberOfBytesRecvd,
             LPDWORD lpFlags,
             LPWSAOVERLAPPED lpOverlapped,
             LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
    DWORD err = GetLastError();
    DWORD wsa_err = WSAGetLastError();
    DWORD ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (called_from_wsock(ret_addr))
        return retval;

    if (lpOverlapped != NULL)
    {
        message_logger_log_message("WSARecv", ret_addr, MESSAGE_CTX_WARNING,
                                   "overlapped I/O not yet supported");
    }

    if (retval == 0)
    {
        for (DWORD i = 0; i < dwBufferCount; i++)
        {
            WSABUF *buf = &lpBuffers[i];

            log_tcp_packet("WSARecv", ret_addr, PACKET_DIRECTION_INCOMING, s,
                           buf->buf, buf->len);
        }
    }
    else if (retval == SOCKET_ERROR)
    {
        if (wsa_err == WSAEWOULDBLOCK)
        {
            message_logger_log_message("WSARecv", ret_addr, MESSAGE_CTX_WARNING,
						               "non-blocking mode not yet supported");
        }

        if (wsa_err != WSAEWOULDBLOCK && wsa_err != WSA_IO_PENDING)
        {
            log_tcp_disconnected("WSARecv", ret_addr, s,
                                 (err == WSAECONNRESET) ? NULL : &err);
        }
    }

    WSASetLastError(wsa_err);
    SetLastError(err);
    return retval;
}

static int __cdecl
WSASend_called(BOOL carry_on,
               DWORD ret_addr,
               SOCKET s,
               LPWSABUF lpBuffers,
               DWORD dwBufferCount,
               LPDWORD lpNumberOfBytesSent,
               DWORD dwFlags,
               LPWSAOVERLAPPED lpOverlapped,
               LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
    return softwall_decide_from_socket("WSASend", ret_addr, s, &carry_on);
}

static int __stdcall
WSASend_done(int retval,
             SOCKET s,
             LPWSABUF lpBuffers,
             DWORD dwBufferCount,
             LPDWORD lpNumberOfBytesSent,
             DWORD dwFlags,
             LPWSAOVERLAPPED lpOverlapped,
             LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
    DWORD err = GetLastError();
    DWORD wsa_err = WSAGetLastError();
    DWORD ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (called_from_wsock(ret_addr))
        return retval;

    if (lpOverlapped != NULL)
    {
        message_logger_log_message("WSASend", ret_addr, MESSAGE_CTX_WARNING,
                                   "overlapped I/O not yet supported");
    }

    if (retval == 0)
    {
        for (DWORD i = 0; i < dwBufferCount; i++)
        {
            WSABUF *buf = &lpBuffers[i];

            log_tcp_packet("WSASend", ret_addr, PACKET_DIRECTION_OUTGOING, s,
                           buf->buf, buf->len);
        }
    }
    else if (retval == SOCKET_ERROR)
    {
        if (wsa_err == WSAEWOULDBLOCK)
        {
	        message_logger_log_message("WSASend", ret_addr, MESSAGE_CTX_WARNING,
							           "non-blocking mode not yet supported");
        }

        if (wsa_err != WSAEWOULDBLOCK && wsa_err != WSA_IO_PENDING)
        {
            log_tcp_disconnected("WSASend", ret_addr, s,
                                 (err == WSAECONNRESET) ? NULL : &err);
        }
    }

    WSASetLastError(wsa_err);
    SetLastError(err);
    return retval;
}

static SOCKET __cdecl
WSAAccept_called(BOOL carry_on,
                 DWORD ret_addr,
                 SOCKET s,
                 struct sockaddr *addr,
                 LPINT addrlen,
                 LPCONDITIONPROC lpfnCondition,
                 DWORD dwCallbackData)
{
    int retval =
        softwall_decide_from_socket_and_remote_address("WSAAccept", ret_addr, s, NULL, &carry_on);

    if (carry_on)
    {
        /* FIXME: only issue this once for non-blocking sockets */
        log_tcp_listening("WSAAccept", ret_addr, s);
    }

    return retval;
}

static SOCKET __stdcall
WSAAccept_done(SOCKET retval,
               SOCKET s,
               struct sockaddr *addr,
               LPINT addrlen,
               LPCONDITIONPROC lpfnCondition,
               DWORD dwCallbackData)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));
    int overridden_retval;
    BOOL carry_on;

    overridden_retval =
        softwall_decide_from_socket_and_remote_address("WSAAccept", ret_addr, s,
                                                       (const sockaddr_in *) addr,
                                                       &carry_on);

    if (!carry_on)
        return overridden_retval;

    if (retval != INVALID_SOCKET)
    {
        log_tcp_client_connected("WSAAccept", ret_addr, s, retval);
    }

    SetLastError(err);
    return retval;
}

static int __cdecl
wsock32_recv_called(BOOL carry_on,
                    DWORD ret_addr,
                    SOCKET s,
                    char *buf,
                    int len,
                    int flags)
{
    return softwall_decide_from_socket("wsock32_recv", ret_addr, s, &carry_on);
}

static int __stdcall
wsock32_recv_done(int retval,
                  SOCKET s,
                  char *buf,
                  int len,
                  int flags)
{
  DWORD err = GetLastError();
  int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

  if (retval > 0)
  {
    log_tcp_packet("wsock32_recv", ret_addr, PACKET_DIRECTION_INCOMING, s, buf, retval);
  }
  else if (retval == 0)
  {
    log_tcp_disconnected("wsock32_recv", ret_addr, s, NULL);
  }
  else if (retval == SOCKET_ERROR)
  {
    if (err != WSAEWOULDBLOCK)
    {
      log_tcp_disconnected("wsock32_recv", ret_addr, s, &err);
    }
  }

  SetLastError(err);
  return retval;
}

HOOK_GLUE_INTERRUPTIBLE(getaddrinfo, (4 * 4))

HOOK_GLUE_INTERRUPTIBLE(closesocket, (1 * 4))
HOOK_GLUE_INTERRUPTIBLE(recv, (4 * 4))
HOOK_GLUE_INTERRUPTIBLE(send, (4 * 4))
HOOK_GLUE_INTERRUPTIBLE(recvfrom, (6 * 4))
HOOK_GLUE_INTERRUPTIBLE(sendto, (6 * 4))
HOOK_GLUE_INTERRUPTIBLE(accept, (3 * 4))
HOOK_GLUE_INTERRUPTIBLE(connect, (3 * 4))
HOOK_GLUE_INTERRUPTIBLE(WSARecv, (7 * 4))
HOOK_GLUE_INTERRUPTIBLE(WSASend, (7 * 4))
HOOK_GLUE_INTERRUPTIBLE(WSAAccept, (5 * 4))
HOOK_GLUE_INTERRUPTIBLE(wsock32_recv, (4 * 4))

void
hook_winsock()
{
    // Hook the Winsock API
    HMODULE h = LoadLibrary("ws2_32.dll");
    if (h == NULL)
    {
	    MessageBox(0, "Failed to load 'ws2_32.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
      return;
    }

    HOOK_FUNCTION(h, getaddrinfo);

    HOOK_FUNCTION(h, closesocket);
    HOOK_FUNCTION(h, recv);
    HOOK_FUNCTION(h, send);
    HOOK_FUNCTION(h, recvfrom);
    HOOK_FUNCTION(h, sendto);
    HOOK_FUNCTION(h, accept);
    HOOK_FUNCTION(h, connect);
    HOOK_FUNCTION(h, WSARecv);
    HOOK_FUNCTION(h, WSASend);
    HOOK_FUNCTION(h, WSAAccept);

    h = LoadLibrary("wsock32.dll");
    if (h == NULL)
    {
        MessageBox(0, "Failed to load 'wsock32.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    if (GetModuleInformation(GetCurrentProcess(), h, &wsock32_info,
                             sizeof(wsock32_info)) == 0)
    {
        message_logger_log_message("DllMain", 0, MESSAGE_CTX_WARNING,
                                   "GetModuleInformation failed with errno %d",
                                   GetLastError());
    }

    HOOK_FUNCTION_BY_ALIAS(h, recv, wsock32_recv);
}