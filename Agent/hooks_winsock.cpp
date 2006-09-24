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
#include <psapi.h>

static MODULEINFO wsock32_info;

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
                               "overlapped I/O not supported");
  }

  if (retval == 0)
  {
    if (dwBufferCount >= 1)
    {
      if (dwBufferCount > 1)
      {
        message_logger_log_message("WSARecv", ret_addr, MESSAGE_CTX_WARNING,
                                   "only dwBufferCount == 1 supported for now");
      }

      log_tcp_packet("WSARecv", ret_addr, PACKET_DIRECTION_INCOMING, s,
                     lpBuffers[0].buf, *lpNumberOfBytesRecvd);
    }
  }
  else if (retval == SOCKET_ERROR)
  {
	if (wsa_err == WSAEWOULDBLOCK)
	{
		message_logger_log_message("WSARecv", ret_addr, MESSAGE_CTX_WARNING,
								   "non-blocking mode not supported");
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
                               "overlapped I/O not supported");
  }

  if (retval == 0)
  {
    if (dwBufferCount >= 1)
    {
      if (dwBufferCount > 1)
      {
        message_logger_log_message("WSASend", ret_addr, MESSAGE_CTX_WARNING,
                                   "only dwBufferCount == 1 supported for now");
      }

      log_tcp_packet("WSASend", ret_addr, PACKET_DIRECTION_OUTGOING, s,
                     lpBuffers[0].buf, *lpNumberOfBytesSent);
    }
  }
  else if (retval == SOCKET_ERROR)
  {
	if (wsa_err == WSAEWOULDBLOCK)
	{
		message_logger_log_message("WSASend", ret_addr, MESSAGE_CTX_WARNING,
								   "non-blocking mode not supported");
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