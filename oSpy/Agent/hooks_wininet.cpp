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
	SIGNATURE_ICSOCKET_RECV_TAIL,
    SIGNATURE_SECURE_SEND_AFTER_ENCRYPT,
	SIGNATURE_SECURE_SEND_AFTER_SEND,
	SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL,
	SIGNATURE_SECURE_RECV_AFTER_DECRYPT,
	SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL,
};

static const FunctionSignature wininet_signatures[] = {
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

	// SIGNATURE_ICSOCKET_RECV_TAIL
	// FIXME
    {
        "wininet.dll",
		-5,
		"E8 3A 87 FF FF"		// call    ?DoFsm@@YGKPAVCFsm@@@Z ; DoFsm(CFsm *)
		"5E"					// pop     esi
		"5D"					// pop     ebp
		"C2 1C 00"				// retn    1Ch
    },

    // SIGNATURE_SECURE_SEND_AFTER_ENCRYPT
    {
        "wininet.dll",
		0,
		"85 C0"					// test    eax, eax
		"89 45 08"				// mov     [ebp+arg_0], eax
		"75 30"					// jnz     short OUT
    },

	// SIGNATURE_SECURE_SEND_AFTER_SEND
	// FIXME
    {
        "wininet.dll",
		0,
		"85 C0"					// test    eax, eax
		"89 45 08"				// mov     [ebp+arg_0], eax
		"75 05"					// jnz     short OUT
    },

	// SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL
	// FIXME (?)
    {
        "wininet.dll",
		-3,
		"FF 50 48"				// call    dword ptr [eax+48h]
		"3B C7"					// test    eax, eax
	},

	// SIGNATURE_SECURE_RECV_AFTER_DECRYPT
	// FIXME (?)
    {
        "wininet.dll",
		0,
		"B9 65 32 00 00"		// mov     ecx, 3265h
	},

	// SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL
	// FIXME (?)
    {
        "wininet.dll",
		-3,
		"FF 50 4C"				// call    dword ptr [eax+4Ch]
		"3B C3"					// cmp     eax, ebx
		"89 45 FC"				// mov     [ebp+var_4], eax
	},
};

static const FunctionSignature wininet_signatures_ie7[] = {
	// SIGNATURE_ICSOCKET_SEND_TAIL
    {
        "wininet.dll",
		-5,
		"E8 68 85 FF FF"		// call    ?DoFsm@@YGKPAVCFsm@@@Z ; DoFsm(CFsm *)
		"5E"					// pop     esi
		"5D"					// pop     ebp
		"C2 0C 00"				// retn    0Ch
    },

	// SIGNATURE_ICSOCKET_SEND_CALL
	{
		"wininet.dll",
		-10,
		"FF 76 78"				// push    dword ptr [esi+78h]
		"53"					// push    ebx
	},

	// SIGNATURE_ICSOCKET_RECV_TAIL
    {
        "wininet.dll",
		-5,
		"E8 3A 87 FF FF"		// call    ?DoFsm@@YGKPAVCFsm@@@Z ; DoFsm(CFsm *)
		"5E"					// pop     esi
		"5D"					// pop     ebp
		"C2 1C 00"				// retn    1Ch
    },

    // SIGNATURE_SECURE_SEND_AFTER_ENCRYPT
    {
        "wininet.dll",
		0,
		"85 C0"					// test    eax, eax
		"89 45 08"				// mov     [ebp+arg_0], eax
		"75 33"					// jnz     short OUT
    },

	// SIGNATURE_SECURE_SEND_AFTER_SEND
    {
        "wininet.dll",
		0,
		"85 C0"					// test    eax, eax
		"89 45 08"				// mov     [ebp+arg_0], eax
		"75 05"					// jnz     short OUT
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

	// SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL
    {
        "wininet.dll",
		-3,
		"FF 50 4C"				// call    dword ptr [eax+4Ch]
		"3B C3"					// cmp     eax, ebx
		"89 45 FC"				// mov     [ebp+var_4], eax
	},
};

typedef struct {
	char *data;
	int dataLen;
} CFsm_SecureSend_Upper;

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
	SOCKET fd;
} ICSocket_Upper;

static int cfsm_securesend_base_size;
static int cfsm_securerecv_base_size;

static int icsocket_base_size;

static OMap<void *, bool>::Type ignored_icsend_ret_addrs;
static OMap<void *, bool>::Type ignored_icrecv_ret_addrs;

static void
handle_icsocket_send(void *parent_ebp, void *self)
{
	void *bt_address = (void *) ((char *) parent_ebp + 0x4);
	void *ret_addr = (void *) *((DWORD *) bt_address);
	
	if (ignored_icsend_ret_addrs.find(ret_addr) == ignored_icsend_ret_addrs.end())
	{
		ICSocket_Upper *sock_upper = (ICSocket_Upper *) ((char *) self + icsocket_base_size);
		char *data = *((char **) ((char *) parent_ebp + 0x8));
		int dataLen = *((int *) ((char *) parent_ebp + 0xc));

		log_tcp_packet("ICSocket::Send", bt_address, PACKET_DIRECTION_OUTGOING, sock_upper->fd, data, dataLen);
	}
}

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
		handle_icsocket_send(parent_ebp, self);
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

static void
handle_icsocket_recv(void *parent_ebp, void *self)
{
	void *bt_address = (void *) ((char *) parent_ebp + 0x4);
	void *ret_addr = (void *) *((DWORD *) bt_address);
	
	if (ignored_icrecv_ret_addrs.find(ret_addr) == ignored_icrecv_ret_addrs.end())
	{
		ICSocket_Upper *sock_upper = (ICSocket_Upper *) ((char *) self + icsocket_base_size);

//unsigned __int32 __thiscall ICSocket__Receive(void *this,void **bufPtr,unsigned __int32 *bufSize,unsigned __int32 *arg3,unsigned __int32 *arg4,unsigned __int32 arg5,unsigned __int32 arg6,int *arg7);

		void **bufPtr = *((void ***) ((char *) parent_ebp + 0x8));
		unsigned int *bufSize = *((unsigned int **) ((char *) parent_ebp + 0xc));
		unsigned int *arg3 = *((unsigned int **) ((char *) parent_ebp + 0x10));
		unsigned int *arg4 = *((unsigned int **) ((char *) parent_ebp + 0x14));
		unsigned int arg5 = *((unsigned int *) ((char *) parent_ebp + 0x18));
		unsigned int arg6 = *((unsigned int *) ((char *) parent_ebp + 0x1c));
		unsigned int *arg7 = *((unsigned int **) ((char *) parent_ebp + 0x20));

		message_logger_log_message("ICSocket::Receive", bt_address, MESSAGE_CTX_INFO,
			"*bufPtr=%p, *bufSize=%d, *arg3=%d, *arg4=%d, arg5=%d, arg6=%d, *arg7=%d",
			*bufPtr, *bufSize, *arg3, *arg4, arg5, arg6, *arg7);
		//log_tcp_packet("ICSocket::Receive", bt_address, PACKET_DIRECTION_INCOMING, sock_upper->fd, data, dataLen);
	}
}

static __declspec(naked) void
icsocket_recv_tail()
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
		handle_icsocket_recv(parent_ebp, self);
	}

    __asm {
        mov		esp, ebp;

		popad;

		/* continue where the original left off */
		pop     esi;
		pop     ebp;
		retn    1Ch;
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
		void *bt_address = (void *) ((char *) parent_ebp + 0x4);
		ICSocket_Upper *sock_upper = (ICSocket_Upper *) ((char *) self + icsocket_base_size);
		CFsm_SecureSend_Upper *fsm_upper = (CFsm_SecureSend_Upper *) ((char *) fsm + cfsm_securesend_base_size);

		log_tcp_packet("SecureSend", bt_address, PACKET_DIRECTION_OUTGOING, sock_upper->fd,
					   fsm_upper->data, fsm_upper->dataLen);
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

static char *secure_recv_after_decrypt_impl = NULL;

static __declspec(naked) void
secure_recv_after_decrypt()
{
	void *parent_ebp;
	void *self; // ICSecureSocket *
	void *fsm; // CFsm_SecureReceive *
    int retval;

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

	if (retval == 0)
	{
		void *bt_address = (void *) ((char *) parent_ebp + 0x4);
		ICSocket_Upper *sock_upper = (ICSocket_Upper *) ((char *) self + icsocket_base_size);
		CFsm_SecureReceive_Upper *fsm_upper = (CFsm_SecureReceive_Upper *) ((char *) fsm + cfsm_securerecv_base_size);

		log_tcp_packet("SecureReceive", bt_address, PACKET_DIRECTION_INCOMING, sock_upper->fd,
					   fsm_upper->data, fsm_upper->dataLen);
	}

    __asm {
        mov		esp, ebp;

		popad;

		/* continue where the original left off */
		mov     ecx, 3265h;

		jmp		[secure_recv_after_decrypt_impl];
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

	const FunctionSignature *sigs = wininet_signatures;
	cfsm_securesend_base_size = 0x74;
	cfsm_securerecv_base_size = 0x94; // FIXME
	icsocket_base_size = 0x18;
	bool found;
	char *error;


	//
	// ICSocket::Send
	//
	found = override_function_by_signature(&sigs[SIGNATURE_ICSOCKET_SEND_TAIL],
										   icsocket_send_tail, NULL, &error);
	if (!found)
	{
		sigs = wininet_signatures_ie7;
		cfsm_securesend_base_size = 0x78;
		cfsm_securerecv_base_size = 0x94;
		icsocket_base_size = 0x1c;

		sspy_free(error);

		found = override_function_by_signature(&sigs[SIGNATURE_ICSOCKET_SEND_TAIL],
											   icsocket_send_tail, NULL, &error);
		if (!found)
		{
			LOG_OVERRIDE_ERROR("SIGNATURE_ICSOCKET_SEND_TAIL", error);
		}
	}

	void *icsocket_send_call_retaddr;

	if (find_signature(&sigs[SIGNATURE_ICSOCKET_SEND_CALL], &icsocket_send_call_retaddr,
				       &error))
	{
		ignored_send_ret_addrs[icsocket_send_call_retaddr] = true;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_ICSOCKET_SEND_CALL", error);
	}

	//
	// ICSocket::Receive
	//

	if (!override_function_by_signature(&sigs[SIGNATURE_ICSOCKET_RECV_TAIL],
										icsocket_recv_tail, NULL, &error))
	{
		LOG_OVERRIDE_ERROR("SIGNATURE_ICSOCKET_RECV_TAIL", error);
	}


	//
	// ICSecureSocket
	//

    if (override_function_by_signature(&sigs[SIGNATURE_SECURE_SEND_AFTER_ENCRYPT],
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

	void *secure_send_icsend_retaddr;

	if (find_signature(&sigs[SIGNATURE_SECURE_SEND_AFTER_SEND],
					   &secure_send_icsend_retaddr, &error))
	{
		ignored_icsend_ret_addrs[secure_send_icsend_retaddr] = true;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_SEND_AFTER_SEND", error);
	}

	void *secure_send_encryptmsg_retaddr;

	if (find_signature(&sigs[SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL],
					   &secure_send_encryptmsg_retaddr, &error))
	{
		ignored_encryptmsg_ret_addrs[secure_send_encryptmsg_retaddr] = true;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL", error);
	}

    if (override_function_by_signature(&sigs[SIGNATURE_SECURE_RECV_AFTER_DECRYPT],
                                       secure_recv_after_decrypt, (LPVOID *) &secure_recv_after_decrypt_impl,
                                       &error))
    {
		// we overwrite 5 bytes with our JMP, so our hook will continue from there
		secure_recv_after_decrypt_impl += 5;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_RECV_AFTER_DECRYPT", error);
    }

	void *secure_recv_decryptmsg_retaddr;

	if (find_signature(&sigs[SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL],
					   &secure_recv_decryptmsg_retaddr, &error))
	{
		ignored_decryptmsg_ret_addrs[secure_recv_decryptmsg_retaddr] = true;
	}
	else
	{
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL", error);
	}
}
