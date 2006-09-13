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
#define SECURITY_WIN32
#include <security.h>

static SECURITY_STATUS __cdecl
EncryptMessage_called(BOOL carry_on,
                      DWORD ret_addr,
                      PCtxtHandle phContext,
                      ULONG fQOP,
                      PSecBufferDesc pMessage,
                      ULONG MessageSeqNo)
{
    unsigned int i;

    for (i = 0; i < pMessage->cBuffers; i++)
    {
        SecBuffer *buffer = &pMessage->pBuffers[i];

        if (buffer->BufferType == SECBUFFER_DATA)
        {
            message_logger_log_packet("EncryptMessage", ret_addr, (DWORD) phContext,
                                      PACKET_DIRECTION_OUTGOING, NULL, NULL,
                                      (const char *) buffer->pvBuffer,
                                      buffer->cbBuffer);
        }
    }

    return 0;
}

static SECURITY_STATUS __stdcall
EncryptMessage_done(SECURITY_STATUS retval,
                    PCtxtHandle phContext,
                    ULONG fQOP,
                    PSecBufferDesc pMessage,
                    ULONG MessageSeqNo)
{
    return retval;
}

static SECURITY_STATUS __cdecl
DecryptMessage_called(BOOL carry_on,
                      DWORD ret_addr,
                      PCtxtHandle phContext,
                      PSecBufferDesc pMessage,
                      ULONG MessageSeqNo,
                      PULONG pfQOP)
{
    return 0;
}

static SECURITY_STATUS __stdcall
DecryptMessage_done(SECURITY_STATUS retval,
                    PCtxtHandle phContext,
                    PSecBufferDesc pMessage,
                    ULONG MessageSeqNo,
                    PULONG pfQOP)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));
    unsigned int i;

    for (i = 0; i < pMessage->cBuffers; i++)
    {
        SecBuffer *buffer = &pMessage->pBuffers[i];

        if (buffer->BufferType == SECBUFFER_DATA)
        {
            message_logger_log_packet("DecryptMessage", ret_addr, (DWORD) phContext,
                                      PACKET_DIRECTION_INCOMING, NULL, NULL,
                                      (const char *) buffer->pvBuffer,
                                      buffer->cbBuffer);
        }
    }

    SetLastError(err);
    return retval;
}

HOOK_GLUE_INTERRUPTIBLE(EncryptMessage, (4 * 4))
HOOK_GLUE_INTERRUPTIBLE(DecryptMessage, (4 * 4))

void
hook_secur32()
{
    // Hook the Secur32 API
    HMODULE h = LoadLibrary("secur32.dll");
    if (h == NULL)
    {
	    MessageBox(0, "Failed to load 'secur32.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    HOOK_FUNCTION(h, EncryptMessage);
    HOOK_FUNCTION(h, DecryptMessage);
}
