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
#include "httpapi_util.h"
#include <http.h>


/*
 * Strategy:
 *
 * Whenever an HTTP request queue is created by calling HttpCreateHttpHandle
 * we intercept that and launch a new thread that keeps monitoring this
 * queue.
 * If the client tries to retrieve data from the monitored queue through
 * HttpReceiveHttpRequest or HttpReceiveRequestEntityBody we intercept this
 * and dispatch those requests internally.
 *
 * WORK IN PROGRESS - do not use ...
 */


#define RECEIVE_BUFFER_SIZE 65536

typedef ULONG (__stdcall *HttpReceiveHttpRequestFunc) (
  HANDLE ReqQueueHandle,
  HTTP_REQUEST_ID RequestId,
  ULONG Flags,
  PHTTP_REQUEST pRequestBuffer,
  ULONG RequestBufferLength,
  PULONG pBytesReceived,
  LPOVERLAPPED pOverlapped
);

static HttpReceiveHttpRequestFunc HttpReceiveHttpRequestImpl;

/* HACK */
static HANDLE cur_req_queue;
static DWORD cur_thread_id;

static DWORD WINAPI
receive_thread(LPVOID lpParameter)
{
    HANDLE queue = (HANDLE) lpParameter;
    ULONG bytes_received, retval;

    while (TRUE)
    {
        HTTP_REQUEST *req = (HTTP_REQUEST *) sspy_malloc(RECEIVE_BUFFER_SIZE);

        retval = HttpReceiveHttpRequestImpl(queue, HTTP_NULL_ID,
            HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY,
            req, RECEIVE_BUFFER_SIZE,
            &bytes_received, NULL);

        if (retval != NO_ERROR)
            break;

        log_http_request(queue, req, NULL, 0);
    }

    message_logger_log_message("receive_thread", 0, MESSAGE_CTX_INFO,
        "exiting with retval=0x%08x", retval);

    return 0;
}


/*
 * HttpCreateHttpHandle
 */
static ULONG __cdecl
HttpCreateHttpHandle_called(BOOL carry_on,
                            DWORD ret_addr,
                            PHANDLE pReqQueueHandle,
                            ULONG Options)
{
    return 0;
}

static ULONG __stdcall
HttpCreateHttpHandle_done(ULONG retval,
                          PHANDLE pReqQueueHandle,
                          ULONG Options)
{
    DWORD last_error = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval == NO_ERROR)
    {
        message_logger_log_message("HttpCreateHttpHandle", (char *) &retval - 4,
            MESSAGE_CTX_WARNING, "starting receive thread for handle %p",
            *pReqQueueHandle);

        cur_req_queue = *pReqQueueHandle;

        CreateThread(NULL, 0, receive_thread, *pReqQueueHandle, 0,
                     &cur_thread_id);
    }

    SetLastError(last_error);
    return retval;
}

/*
 * HttpReceiveHttpRequest
 */
static ULONG __cdecl
HttpReceiveHttpRequest_called(BOOL carry_on,
                              DWORD ret_addr,
                              HANDLE ReqQueueHandle,
                              HTTP_REQUEST_ID RequestId,
                              ULONG Flags,
                              PHTTP_REQUEST pRequestBuffer,
                              ULONG RequestBufferLength,
                              PULONG pBytesReceived,
                              LPOVERLAPPED pOverlapped)
{
    if (GetCurrentThreadId() != cur_thread_id && ReqQueueHandle == cur_req_queue)
    {
        carry_on = FALSE;
        return ERROR_IO_PENDING; /* evil evil */
    }

    return 0;
}

static ULONG __stdcall
HttpReceiveHttpRequest_done(ULONG retval,
                            HANDLE ReqQueueHandle,
                            HTTP_REQUEST_ID RequestId,
                            ULONG Flags,
                            PHTTP_REQUEST pRequestBuffer,
                            ULONG RequestBufferLength,
                            PULONG pBytesReceived,
                            LPOVERLAPPED pOverlapped)
{
    DWORD last_error = GetLastError();

    SetLastError(last_error);
    return retval;
}

/*
 * HttpReceiveRequestEntityBody
 */
static ULONG __cdecl
HttpReceiveRequestEntityBody_called(BOOL carry_on,
                                    DWORD ret_addr,
                                    HANDLE ReqQueueHandle,
                                    HTTP_REQUEST_ID RequestId,
                                    ULONG Flags,
                                    PVOID pBuffer,
                                    ULONG BufferLength,
                                    PULONG pBytesReceived,
                                    LPOVERLAPPED pOverlapped)
{
    if (GetCurrentThreadId() != cur_thread_id && ReqQueueHandle == cur_req_queue)
    {
        carry_on = FALSE;
        return ERROR_IO_PENDING; /* evil evil */
    }

    return 0;
}

static ULONG __stdcall
HttpReceiveRequestEntityBody_done(ULONG retval,
                                  HANDLE ReqQueueHandle,
                                  HTTP_REQUEST_ID RequestId,
                                  ULONG Flags,
                                  PVOID pBuffer,
                                  ULONG BufferLength,
                                  PULONG pBytesReceived,
                                  LPOVERLAPPED pOverlapped)
{
    DWORD last_error = GetLastError();

    SetLastError(last_error);
    return retval;
}

/*
 * GetOverlappedResult
 */
static BOOL __cdecl
GetOverlappedResult_called(BOOL carry_on,
                           DWORD ret_addr,
                           HANDLE hFile,
                           LPOVERLAPPED lpOverlapped,
                           LPDWORD lpNumberOfBytesTransferred,
                           BOOL bWait)
{
    return TRUE;
}

static BOOL __stdcall
GetOverlappedResult_done(BOOL retval,
                         HANDLE hFile,
                         LPOVERLAPPED lpOverlapped,
                         LPDWORD lpNumberOfBytesTransferred,
                         BOOL bWait)
{
    DWORD last_error = GetLastError();

    SetLastError(last_error);
    return retval;
}

HOOK_GLUE_INTERRUPTIBLE(HttpCreateHttpHandle, (2 * 4));
HOOK_GLUE_INTERRUPTIBLE(HttpReceiveHttpRequest, ((6 * 4) + (1 * 8)))
HOOK_GLUE_INTERRUPTIBLE(HttpReceiveRequestEntityBody, ((6 * 4) + (1 * 8)))

HOOK_GLUE_INTERRUPTIBLE(GetOverlappedResult, (4 * 4))

void
hook_httpapi()
{
    HMODULE h;

    h = LoadLibrary("kernel32.dll");
    if (h == NULL)
    {
        MessageBox(0, "Failed to load 'kernel32.dll'.",
            "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    HOOK_FUNCTION(h, GetOverlappedResult);

    h = LoadLibrary("httpapi.dll");
    if (h == NULL)
    {
        MessageBox(0, "Failed to load 'httpapi.dll'.",
            "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    HttpReceiveHttpRequestImpl = (HttpReceiveHttpRequestFunc)
        GetProcAddress(h, "HttpReceiveHttpRequest");

    HOOK_FUNCTION(h, HttpCreateHttpHandle);
    HOOK_FUNCTION(h, HttpReceiveHttpRequest);
    HOOK_FUNCTION(h, HttpReceiveRequestEntityBody);
}
