//
// Copyright (c) 2006-2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#include "stdafx.h"
#include "hooking.h"
#include "logging_old.h"
#define SECURITY_WIN32
#include <security.h>
#include <psapi.h>

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
