/**
 * Copyright (C) 2007  Ole André Vadla Ravnås <oleavr@gmail.com>
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
#include "hooks.h"

typedef enum {
	SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET = 0,
	SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV,
	SIGNATURE_ICSOCKET_SEND_START_SEND,
	SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL,
	SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL,
	SIGNATURE_SECURE_RECV_AFTER_DECRYPT,
	SIGNATURE_SECURE_SEND_AFTER_ENCRYPT,
};

static const FunctionSignature signatures[] = {
	// SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET
	{
		"wininet.dll",
		-33,
		"81 4E 28 00 00 01 00"	// or      dword ptr [esi+28h], 10000h
		"89 86 ?? ?? ?? ??"		// mov     [esi+0C8h], eax
		"89 86 ?? ?? ?? ??"		// mov     [esi+0CCh], eax
		"89 86 ?? ?? ?? ??"		// mov     [esi+0D0h], eax
		"89 86 ?? ?? ?? ??"		// mov     [esi+0D8h], eax
		"C7 06 ?? ?? ?? ??"		// mov     dword ptr [esi], offset ??_7ICSecureSocket@@6B@ ; const ICSecureSocket::`vftable'
	},

	// SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV
    {
        "wininet.dll",
		-21,
		"6A 00"					// push    0
		"FF B6 ?? 00 00 00"		// push    [esi+CFsm_SocketReceive.dataLen]
		"FF B6 ?? 00 00 00"		// push    [esi+CFsm_SocketReceive.recvBuf]
		"53"					// push    ebx
		"FF 15 ?? ?? ?? ??"		// call    __I_recv
    },

	// SIGNATURE_ICSOCKET_SEND_START_SEND
    {
        "wininet.dll",
		-13,
		"6A 00"					// push    0
		"50"					// push    eax
		"FF 76 78"				// push    dword ptr [esi+78h]
		"53"					// push    ebx
		"FF 15 ?? ?? ?? ??"		// call    __I_send
    },

	// SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL
    {
        "wininet.dll",
		-3,
		"FF 50 4C"				// call    dword ptr [eax+4Ch]
		"3B C3"					// cmp     eax, ebx
		"89 45 FC"				// mov     [ebp+var_4], eax
	},

	// SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL
    {
        "wininet.dll",
		-3,
		"FF 50 48"				// call    dword ptr [eax+48h]
		"85 C0"					// test    eax, eax
	},

	// SIGNATURE_SECURE_RECV_AFTER_DECRYPT
    {
        "wininet.dll",
		0,
		"B9 65 32 00 00"		// mov     ecx, 3265h
	},

    // SIGNATURE_SECURE_SEND_AFTER_ENCRYPT
    {
        "wininet.dll",
		0,
		"85 C0"					// test    eax, eax
		"89 45 08"				// mov     [ebp+arg_0], eax
		"75 33"					// jnz     short OUT
    },
};

typedef struct {
	void *vtable;
} ICSocket_base;

typedef struct {
	SOCKET fd;
} ICSocket_Upper;

typedef struct {
	char *data;
	char *recvBuf;
	int unk1;		// observed to be 0
	int dataLen;
	int bytesReceived1;
	int bytesReceived2;
	int padding2[9];
	int unk4;		// observed to be 0
} CFsm_SecureReceive_Upper;

typedef struct {
	char *data;
	int dataLen;
} CFsm_SecureSend_Upper;

static void *g_ICSecureSocketVTable = NULL;

static unsigned int g_icsocketBaseSize = 0;
static unsigned int g_cfsmSecureRecvBaseSize = 0;
static unsigned int g_cfsmSecureSendBaseSize = 0;
static char *g_secureSendAfterDecryptImpl = NULL;
static char *g_secureSendAfterEncryptImpl = NULL;

static bool
ICSocketReceiveContinue_ShouldLog(CpuContext *context, va_list args)
{
	ICSocket_base *self = *((ICSocket_base **) (context->ebp - 4));
	return (self->vtable != g_ICSecureSocketVTable);
}

static bool
ICSocketSendStart_ShouldLog(CpuContext *context, va_list args)
{
	ICSocket_base *self = *((ICSocket_base **) (context->ebp - 4));
	return (self->vtable != g_ICSecureSocketVTable);
}

static __declspec(naked) void
SecureReceiveAfterDecrypt()
{
	void *parent_ebp;
	void *self; // ICSecureSocket *
	void *fsm; // CFsm_SecureReceive *
    int retval;
	void *bt_address;
	ICSocket_Upper *sock_upper;
	CFsm_SecureReceive_Upper *fsm_upper;

    __asm {
		pushad;

		mov		ecx, ebp; // store parent's ebp

        mov		ebp, esp;
        sub		esp, __LOCAL_SIZE;

		mov		[parent_ebp], ecx;
		mov		[self], edi;
		mov		[fsm], esi;
		mov		[retval], eax;
    }

	bt_address = (void *) ((char *) parent_ebp + 0x4);
	sock_upper = (ICSocket_Upper *) ((char *) self + g_icsocketBaseSize);
	fsm_upper = (CFsm_SecureReceive_Upper *) ((char *) fsm + g_cfsmSecureRecvBaseSize);

	if (fsm_upper->dataLen > 0)
	{
		DWORD origLastError = GetLastError();

		log_tcp_packet("SecureReceive", bt_address, PACKET_DIRECTION_INCOMING, sock_upper->fd,
					   fsm_upper->data, fsm_upper->dataLen);

		SetLastError(origLastError);
	}

    __asm {
        mov		esp, ebp;

		popad;

		/* continue where the original left off */
		mov     ecx, 3265h;

		jmp		[g_secureSendAfterDecryptImpl];
    }
}

static __declspec(naked) void
SecureSendAfterEncrypt()
{
	void *parent_ebp;
	void *self; // ICSecureSocket *
	void *fsm; // CFsm_SecureSend *
    int retval;
	void *bt_address;
	ICSocket_Upper *sock_upper;
	CFsm_SecureSend_Upper *fsm_upper;

    __asm {
		pushad;

		mov		edi, ebp; // store parent's ebp

        mov		ebp, esp;
        sub		esp, __LOCAL_SIZE;

		mov		[parent_ebp], edi;
		mov		[fsm], esi;
		mov		[retval], eax;
    }

	self = *((void **) ((char *) parent_ebp - 0x4));
	bt_address = (void *) ((char *) parent_ebp + 0x4);
	sock_upper = (ICSocket_Upper *) ((char *) self + g_icsocketBaseSize);
	fsm_upper = (CFsm_SecureSend_Upper *) ((char *) fsm + g_cfsmSecureSendBaseSize);

	if (fsm_upper->dataLen > 0)
	{
		DWORD origLastError = GetLastError();

		log_tcp_packet("SecureSend", bt_address, PACKET_DIRECTION_OUTGOING, sock_upper->fd,
					   fsm_upper->data, fsm_upper->dataLen);

		SetLastError(origLastError);
	}

    __asm {
        mov		esp, ebp;

		popad;

		/* continue where the original left off */
		test    eax, eax;
		mov     [ebp+8], eax;

		jmp		[g_secureSendAfterEncryptImpl];
    }
}

#define LOG_OVERRIDE_ERROR(sig, e) \
            message_logger_log_message("hook_wininet", 0, MESSAGE_CTX_ERROR,\
                "override_function_by_signature for " sig " failed: %s", e);\
            sspy_free(e)

void
hook_wininet()
{
    HMODULE h = LoadLibrary("wininet.dll");
    if (h == NULL)
    {
	    MessageBox(0, "Failed to load 'wininet.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
		return;
    }

	g_icsocketBaseSize = 0x1c;
	g_cfsmSecureRecvBaseSize = 0x94;
	g_cfsmSecureSendBaseSize = 0x78;

	char *error;
	void *retAddr;
	void **vtableAddr;

	if (find_signature(&signatures[SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET],
					   (LPVOID *) &vtableAddr, &error))
	{
		g_ICSecureSocketVTable = *vtableAddr;

		if (find_signature(&signatures[SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV],
						   &retAddr, &error))
		{
			g_recvHookContext.RegisterReturnAddress(retAddr, ICSocketReceiveContinue_ShouldLog);
		}
		else
		{
			LOG_OVERRIDE_ERROR("SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV", error);
		}

		if (find_signature(&signatures[SIGNATURE_ICSOCKET_SEND_START_SEND],
						   &retAddr, &error))
		{
			g_sendHookContext.RegisterReturnAddress(retAddr, ICSocketSendStart_ShouldLog);
		}
		else
		{
			LOG_OVERRIDE_ERROR("SIGNATURE_ICSOCKET_SEND_START_SEND", error);
		}			
	}
	else
	{
		LOG_OVERRIDE_ERROR("SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET", error);
	}

	if (find_signature(&signatures[SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL],
					   &retAddr, &error))
	{
		g_decryptMessageHookContext.RegisterReturnAddress(retAddr);
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL", error);
	}

	if (find_signature(&signatures[SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL],
					   &retAddr, &error))
	{
		g_encryptMessageHookContext.RegisterReturnAddress(retAddr);
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL", error);
	}

	if (override_function_by_signature(&signatures[SIGNATURE_SECURE_RECV_AFTER_DECRYPT],
                                       SecureReceiveAfterDecrypt, (LPVOID *) &g_secureSendAfterDecryptImpl,
                                       &error))
    {
		// we overwrite 5 bytes with our JMP, so our hook will continue from there
		g_secureSendAfterDecryptImpl += 5;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_RECV_AFTER_DECRYPT", error);
    }

	if (override_function_by_signature(&signatures[SIGNATURE_SECURE_SEND_AFTER_ENCRYPT],
                                       SecureSendAfterEncrypt, (LPVOID *) &g_secureSendAfterEncryptImpl,
                                       &error))
    {
		// we overwrite 5 bytes with our JMP, so our hook will continue from there
		g_secureSendAfterEncryptImpl += 5;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_SEND_AFTER_ENCRYPT", error);
    }
}
