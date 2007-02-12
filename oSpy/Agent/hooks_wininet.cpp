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
	SIGNATURE_ICSOCKET_SEND_TAIL = 0,
	SIGNATURE_ICSOCKET_SEND_CALL,
    SIGNATURE_SECURE_SEND_AFTER_ENCRYPT,
	SIGNATURE_ENCRYPT_MESSAGE_CALL,
};

static FunctionSignature wininet_signatures[] = {
	// SIGNATURE_ICSOCKET_SEND_TAIL
    {
        "wininet.dll",
		-5,
		"E8 93 5C FF FF"		// call    ?DoFsm@@YGKPAVCFsm@@@Z ; DoFsm(CFsm *)
		"5E"					// pop     esi
		"5D"					// pop     ebp
		"C2 0C 00"				// retn    0Ch
    },

	// SIGNATURE_ICSOCKET_SEND_CALL
	{
		"wininet.dll",
		-12,
		"FF 76 74"				// push    dword ptr [esi+74h]
		"FF 77 18"				// push    dword ptr [edi+18h]
	},

    // SIGNATURE_SECURE_SEND_AFTER_ENCRYPT
    {
        "wininet.dll",
		0,
		"85 C0"					// test    eax, eax
		"89 45 08"				// mov     [ebp+arg_0], eax
		"75 30"					// jnz     short OUT
    },

	// SIGNATURE_ENCRYPT_MESSAGE_CALL
    {
        "wininet.dll",
		0,
		"FF 50 48"				// call    dword ptr [eax+48h]
		"3B C7"					// cmp     eax, edi
	},
};

static char *icsocket_send_tail_impl = NULL;

static __declspec(naked) void
icsocket_send_tail()
{
	void *parent_ebp;
	void *self; // ICSocket *
    int retval;

    __asm {
		pushad;

		mov		edi, ebp; // store parent's ebp

        mov		ebp, esp;
        sub		esp, __LOCAL_SIZE;

		mov		[parent_ebp], edi;
		mov		[self], esi;
		mov		[retval], eax;
    }

	if (retval == 0)
	{
		void *bt_address = (void *) *((DWORD *) ((char *) parent_ebp + 0x4));
		SOCKET sock = *((SOCKET *) ((char *) self + 0x18));
		char *data = *((char **) ((char *) parent_ebp + 0x8));
		int dataLen = *((int *) ((char *) parent_ebp + 0xc));

		log_tcp_packet("ICSocket::Send", bt_address, PACKET_DIRECTION_OUTGOING, sock, data, dataLen);
	}

    __asm {
        mov		esp, ebp;

		popad;

		/* continue where the original left off */
		pop     esi;
		pop     ebp;
		retn    0Ch;
    }
}

static char *secure_send_after_encrypt_impl = NULL;

static __declspec(naked) void
secure_send_after_encrypt()
{
	void *parent_ebp;
	void *self; // ICSecureSocket *
	void *fsm; // CFsm_SecureSend *
    int retval;

    __asm {
		pushad;

		mov		edi, ebp; // store parent's ebp

        mov		ebp, esp;
        sub		esp, __LOCAL_SIZE;

		mov		[parent_ebp], edi;
		mov		[fsm], esi;
		mov		[retval], eax;
    }

	if (retval == 0)
	{
		self = *((void **) ((char *) parent_ebp - 0x4));

		void *bt_address = (void *) *((DWORD *) ((char *) parent_ebp + 0x4));
		SOCKET sock = *((SOCKET *) ((char *) self + 0x18));
		char *data = *((char **) ((char *) fsm + 0x74));
		int dataLen = *((int *) ((char *) fsm + 0x78));

		log_tcp_packet("SecureSend", bt_address, PACKET_DIRECTION_OUTGOING, sock, data, dataLen);
	}

    __asm {
        mov		esp, ebp;

		popad;

		/* continue where the original left off */
		test    eax, eax;
		mov     [ebp+8], eax;

		jmp		[secure_send_after_encrypt_impl];
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

	char *error;

	if (override_function_by_signature(&wininet_signatures[SIGNATURE_ICSOCKET_SEND_TAIL],
                                       icsocket_send_tail, (LPVOID *) &icsocket_send_tail_impl,
                                       &error))
	{
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_ICSOCKET_SEND_TAIL", error);
	}

	void *icsocket_send_call_retaddr;

	if (find_signature(&wininet_signatures[SIGNATURE_ICSOCKET_SEND_CALL], &icsocket_send_call_retaddr,
					   &error))
	{
		ignored_send_ret_addrs[icsocket_send_call_retaddr] = true;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_ICSOCKET_SEND_CALL", error);
	}

    if (override_function_by_signature(&wininet_signatures[SIGNATURE_SECURE_SEND_AFTER_ENCRYPT],
                                       secure_send_after_encrypt, (LPVOID *) &secure_send_after_encrypt_impl,
                                       &error))
    {
		// we overwrite 5 bytes with our JMP, so our hook will continue from there
		secure_send_after_encrypt_impl += 5;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_SEND_AFTER_ENCRYPT", error);
    }
}
