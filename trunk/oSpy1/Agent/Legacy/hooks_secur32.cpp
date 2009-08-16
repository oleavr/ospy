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
#include <psapi.h>

#pragma managed(push, off)

static MODULEINFO rpcrt4_info;

static BOOL
called_internally(void *ret_addr)
{
    if ((DWORD) ret_addr >= (DWORD) rpcrt4_info.lpBaseOfDll &&
        (DWORD) ret_addr < (DWORD) rpcrt4_info.lpBaseOfDll + rpcrt4_info.SizeOfImage)
    {
        return TRUE;
    }

    return FALSE;
}

static ContextTracker<PCtxtHandle> tracker;

CHookContext g_encryptMessageHookContext;
CHookContext g_decryptMessageHookContext;

#define ENCRYPT_MESSAGE_ARGS_SIZE (4 * 4)

static SECURITY_STATUS __cdecl
DeleteSecurityContext_called(BOOL carry_on,
                             DWORD ret_addr,
                             PCtxtHandle phContext)
{
    tracker.RemoveContextID(phContext);
    return 0;
}

static SECURITY_STATUS __stdcall
DeleteSecurityContext_done(SECURITY_STATUS retval,
                           PCtxtHandle phContext)
{
    return retval;
}

static SECURITY_STATUS __cdecl
EncryptMessage_called(BOOL carry_on,
                      CpuContext ctx_before,
                      void *bt_addr,
                      void *ret_addr,
                      PCtxtHandle phContext,
                      ULONG fQOP,
                      PSecBufferDesc pMessage,
                      ULONG MessageSeqNo)
{
    if (g_encryptMessageHookContext.ShouldLog(ret_addr, &ctx_before)
        && !called_internally(ret_addr))
    {
        for (unsigned int i = 0; i < pMessage->cBuffers; i++)
        {
            SecBuffer *buffer = &pMessage->pBuffers[i];

            if (buffer->BufferType == SECBUFFER_DATA)
            {
                message_logger_log_packet("EncryptMessage", bt_addr,
                                          tracker.GetContextID(phContext),
                                          PACKET_DIRECTION_OUTGOING, NULL, NULL,
                                          (const char *) buffer->pvBuffer,
                                          buffer->cbBuffer);
            }
        }
    }

    return 0;
}

static SECURITY_STATUS __stdcall
EncryptMessage_done(SECURITY_STATUS retval,
                    CpuContext ctx_after,
                    CpuContext ctx_before,
                    void *bt_addr,
                    void *ret_addr,
                    PCtxtHandle phContext,
                    ULONG fQOP,
                    PSecBufferDesc pMessage,
                    ULONG MessageSeqNo)
{
    return retval;
}

static SECURITY_STATUS __cdecl
DecryptMessage_called(BOOL carry_on,
                      CpuContext ctx_before,
                      void *bt_addr,
                      void *ret_addr,
                      PCtxtHandle phContext,
                      PSecBufferDesc pMessage,
                      ULONG MessageSeqNo,
                      PULONG pfQOP)
{
    return 0;
}

static SECURITY_STATUS __stdcall
DecryptMessage_done(SECURITY_STATUS retval,
                    CpuContext ctx_after,
                    CpuContext ctx_before,
                    void *bt_addr,
                    void *ret_addr,
                    PCtxtHandle phContext,
                    PSecBufferDesc pMessage,
                    ULONG MessageSeqNo,
                    PULONG pfQOP)
{
    DWORD err = GetLastError();
    unsigned int i;

    if (g_decryptMessageHookContext.ShouldLog(ret_addr, &ctx_before)
        && !called_internally(ret_addr))
    {
        for (i = 0; i < pMessage->cBuffers; i++)
        {
            SecBuffer *buffer = &pMessage->pBuffers[i];

            if (buffer->BufferType == SECBUFFER_DATA)
            {
                message_logger_log_packet("DecryptMessage", (char *) &retval - 4,
                                          tracker.GetContextID(phContext),
                                          PACKET_DIRECTION_INCOMING, NULL, NULL,
                                          (const char *) buffer->pvBuffer,
                                          buffer->cbBuffer);
            }
        }
    }

    SetLastError(err);
    return retval;
}

HOOK_GLUE_INTERRUPTIBLE(DeleteSecurityContext, (1 * 4))

HOOK_GLUE_EXTENDED(EncryptMessage, ENCRYPT_MESSAGE_ARGS_SIZE)
HOOK_GLUE_EXTENDED(DecryptMessage, (4 * 4))

void
hook_secur32()
{
    // We don't want to log calls from the RPCRT4 API
    HMODULE h = LoadLibrary("RPCRT4.dll");
    if (h == NULL)
    {
        MessageBox(0, "Failed to load 'RPCRT4.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    if (GetModuleInformation(GetCurrentProcess(), h, &rpcrt4_info,
                             sizeof(rpcrt4_info)) == 0)
    {
        message_logger_log_message("hook_secur32", 0, MESSAGE_CTX_WARNING,
                                   "GetModuleInformation failed with errno %d",
                                   GetLastError());
    }

    // Hook the Secur32 API
    h = LoadLibrary("secur32.dll");
    if (h == NULL)
    {
        MessageBox(0, "Failed to load 'secur32.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    HOOK_FUNCTION(h, DeleteSecurityContext);

    HOOK_FUNCTION(h, EncryptMessage);
    HOOK_FUNCTION(h, DecryptMessage);
}

#pragma managed(pop)
