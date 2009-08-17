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

#pragma managed(push, off)

typedef enum {
    SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET = 0,
    SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV,
    SIGNATURE_ICSOCKET_SEND_START_SEND,
    SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL,
    SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL,
    SIGNATURE_SECURE_RECV_AFTER_DECRYPT,
    SIGNATURE_SECURE_SEND_AFTER_ENCRYPT,
    SIGNATURE_ICASYNCTHREAD_MY_GETADDR,
    SIGNATURE_MY_GETADDR_GETADDRINFO,
    SIGNATURE_ICASYNCTHREAD_CONNECT,
};

static const FunctionSignature signatures_ie6[] = {
    // SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET
    {
        "wininet.dll",
        -24,
        "80 4E 1E 01"           // or      byte ptr [esi+1Eh], 1
        "89 46 38"              // mov     [esi+38h], eax
        "89 46 34"              // mov     [esi+34h], eax
        "89 46 3C"              // mov     [esi+3Ch], eax
        "89 46 40"              // mov     [esi+40h], eax
        "89 46 44"              // mov     [esi+44h], eax
        "89 46 4C"              // mov     [esi+4Ch], eax
        "C7 06 ?? ?? ?? ??"     // mov     dword ptr [esi], offset ??_7ICSecureSocket@@6B@ ; const ICSecureSocket::`vftable'
    },

    // SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV
    {
        "wininet.dll",
        -22,
        "53"                    // push    ebx
        "FF B6 ?? 00 00 00"     // push    dword ptr [esi+9Ch]
        "FF B6 ?? 00 00 00"     // push    dword ptr [esi+94h]
        "FF 70 ??"              // push    dword ptr [eax+18h]
        "FF 15 ?? ?? ?? ??"     // call    __I_recv
    },

    // SIGNATURE_ICSOCKET_SEND_START_SEND
    {
        "wininet.dll",
        -15,
        "6A 00"                 // push    0
        "50"                    // push    eax
        "FF 76 ??"              // push    dword ptr [esi+74h]
        "FF 77 ??"              // push    dword ptr [edi+18h]
        "FF 15 ?? ?? ?? ??"     // call    __I_send
    },

    // SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL
    {
        "wininet.dll",
        -3,
        "FF 50 4C"              // call    dword ptr [eax+4Ch]
        "3B C3"                 // cmp     eax, ebx
        "89 45 FC"              // mov     [ebp+var_4], eax
    },

    // SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL
    {
        "wininet.dll",
        -3,
        "FF 50 48"              // call    dword ptr [eax+48h]
        "3B C7"                 // cmp     eax, edi
    },

    // SIGNATURE_SECURE_RECV_AFTER_DECRYPT
    {
        "wininet.dll",
        0,
        "3D 18 03 09 80"        // cmp     eax, 80090318h
        "89 86 ?? ?? ?? ??"     // mov     [esi+0B0h], eax
    },

    // SIGNATURE_SECURE_SEND_AFTER_ENCRYPT
    {
        "wininet.dll",
        0,
        "85 C0"                 // test    eax, eax
        "89 45 08"              // mov     [ebp+arg_0], eax
        "75 30"                 // jnz     short OUT
    },

    // SIGNATURE_ICASYNCTHREAD_MY_GETADDR -- not in ie6
    {
        "wininet.dll",
        0,
        ""
    },

    // SIGNATURE_MY_GETADDR_GETADDRINFO -- not in ie6
    {
        "wininet.dll",
        0,
        ""
    },

    // SIGNATURE_ICASYNCTHREAD_CONNECT
    {
        "wininet.dll",
        0,
        "83 F8 FF"              // cmp     eax, 0FFFFFFFFh
        "74 16"                 // jz      short loc_771D8FFF
        "8B 45 D8"              // mov     eax, [ebp+var_28]
    },
};

static const FunctionSignature signatures_ie7[] = {
    // SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET
    {
        "wininet.dll",
        -33,
        "81 4E 28 00 00 01 00"  // or      dword ptr [esi+28h], 10000h
        "89 86 ?? ?? ?? ??"     // mov     [esi+0C8h], eax
        "89 86 ?? ?? ?? ??"     // mov     [esi+0CCh], eax
        "89 86 ?? ?? ?? ??"     // mov     [esi+0D0h], eax
        "89 86 ?? ?? ?? ??"     // mov     [esi+0D8h], eax
        "C7 06 ?? ?? ?? ??"     // mov     dword ptr [esi], offset ??_7ICSecureSocket@@6B@ ; const ICSecureSocket::`vftable'
    },

    // SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV
    {
        "wininet.dll",
        -21,
        "6A 00"                 // push    0
        "FF B6 ?? 00 00 00"     // push    [esi+CFsm_SocketReceive.dataLen]
        "FF B6 ?? 00 00 00"     // push    [esi+CFsm_SocketReceive.recvBuf]
        "53"                    // push    ebx
        "FF 15 ?? ?? ?? ??"     // call    __I_recv
    },

    // SIGNATURE_ICSOCKET_SEND_START_SEND
    {
        "wininet.dll",
        -13,
        "6A 00"                 // push    0
        "50"                    // push    eax
        "FF 76 78"              // push    dword ptr [esi+78h]
        "53"                    // push    ebx
        "FF 15 ?? ?? ?? ??"     // call    __I_send
    },

    // SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL
    {
        "wininet.dll",
        -3,
        "FF 50 4C"              // call    dword ptr [eax+4Ch]
        "3B C3"                 // cmp     eax, ebx
        "89 45 FC"              // mov     [ebp+var_4], eax
    },

    // SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL
    {
        "wininet.dll",
        -3,
        "FF 50 48"              // call    dword ptr [eax+48h]
        "85 C0"                 // test    eax, eax
    },

    // SIGNATURE_SECURE_RECV_AFTER_DECRYPT
    {
        "wininet.dll",
        0,
        "B9 65 32 00 00"        // mov     ecx, 3265h
    },

    // SIGNATURE_SECURE_SEND_AFTER_ENCRYPT
    {
        "wininet.dll",
        0,
        "85 C0"                 // test    eax, eax
        "89 45 08"              // mov     [ebp+arg_0], eax
        "75 ??"                 // jnz     short OUT
        "8B 1B"                 // mov     ebx, [ebx]
        "?? ??"                 // FIXME: work around a bug in the signature matcher :P
    },

    // SIGNATURE_ICASYNCTHREAD_MY_GETADDR
    {
        "wininet.dll",
        0,
        "8B F0"                 // mov     esi, eax
        "85 F6"                 // test    esi, esi
        "0F 85 ?? ?? ?? 00"     // jnz     loc_771FCD41
        "8B 85 ?? ?? ?? ??"     // mov     eax, [ebp+var_88]
        "6A 11"                 // push    11h
    },

    // SIGNATURE_MY_GETADDR_GETADDRINFO
    {
        "wininet.dll",
        0,
        "89 45 14"              // mov     [ebp+arg_C], eax
        "A1 ?? ?? ?? ??"        // mov     eax, _WPP_GLOBAL_Control
    },

    // SIGNATURE_ICASYNCTHREAD_CONNECT
    {
        "wininet.dll",
        0,
        "3B C6"                 // cmp     eax, esi
        "74 30"                 // jz      short loc_771CACAC
        "8B 85 6C FF FF FF"     // mov     eax, [ebp+var_94]
    },
};

static const FunctionSignature signatures_ie8[] = {
    // SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET
    {
        "wininet.dll",
        -33,
        "81 4E 28 00 00 01 00"  // or      dword ptr [esi+28h], 10000h
        "89 86 ?? ?? ?? ??"     // mov     [esi+0C8h], eax
        "89 86 ?? ?? ?? ??"     // mov     [esi+0CCh], eax
        "89 86 ?? ?? ?? ??"     // mov     [esi+0D0h], eax
        "89 86 ?? ?? ?? ??"     // mov     [esi+0D8h], eax
        "C7 06 ?? ?? ?? ??"     // mov     dword ptr [esi], offset ??_7ICSecureSocket@@6B@ ; const ICSecureSocket::`vftable'
    },

    // SIGNATURE_ICSOCKET_RECEIVE_CONTINUE_RECV
    {
        "wininet.dll",
        -21,
        "6A 00"                 // push    0
        "FF B6 ?? 00 00 00"     // push    [esi+CFsm_SocketReceive.dataLen]
        "FF B6 ?? 00 00 00"     // push    [esi+CFsm_SocketReceive.recvBuf]
        "53"                    // push    ebx
        "FF 15 ?? ?? ?? ??"     // call    __I_recv
    },

    // SIGNATURE_ICSOCKET_SEND_START_SEND
    {
        "wininet.dll",
        -13,
        "6A 00"                 // push    0
        "50"                    // push    eax
        "FF 76 78"              // push    dword ptr [esi+78h]
        "53"                    // push    ebx
        "FF 15 ?? ?? ?? ??"     // call    __I_send
    },

    // SIGNATURE_SECURE_RECV_DECRYPTMESSAGE_CALL
    {
        "wininet.dll",
        -3,
        "FF 50 4C"              // call    dword ptr [eax+4Ch]
        "89 45 FC"              // mov     [ebp+var_4], eax
        "3B C3"                 // cmp     eax, ebx
    },

    // SIGNATURE_SECURE_SEND_ENCRYPTMESSAGE_CALL
    {
        "wininet.dll",
        -3,
        "FF 50 48"              // call    dword ptr [eax+48h]
        "85 C0"                 // test    eax, eax
    },

    // SIGNATURE_SECURE_RECV_AFTER_DECRYPT
    {
        "wininet.dll",
        0,
        "89 86 ?? 00 00 00"     // mov     [esi+0B4h], eax
        "3D 65 32 00 00"        // cmp     eax, 3265h
    },

    // SIGNATURE_SECURE_SEND_AFTER_ENCRYPT
    {
        "wininet.dll",
        0,
        "89 45 08"              // mov     [ebp+arg_0], eax
        "85 C0"                 // test    eax, eax
        "75 ??"                 // jnz     short OUT
        "8B 1B"                 // mov     ebx, [ebx]
        "?? ??"                 // FIXME: work around a bug in the signature matcher :P
    },

    // SIGNATURE_ICASYNCTHREAD_MY_GETADDR
    {
        "wininet.dll",
        0,
        "8B F0"                 // mov     esi, eax
        "85 F6"                 // test    esi, esi
        "0F 85 ?? ?? ?? 00"     // jnz     loc_771FCD41
        "8B 85 ?? ?? ?? ??"     // mov     eax, [ebp+var_88]
        "6A 11"                 // push    11h
    },

    // SIGNATURE_MY_GETADDR_GETADDRINFO
    {
        "wininet.dll",
        0,
        "89 45 14"              // mov     [ebp+arg_C], eax
        "A1 ?? ?? ?? ??"        // mov     eax, _WPP_GLOBAL_Control
    },

    // SIGNATURE_ICASYNCTHREAD_CONNECT
    {
        "wininet.dll",
        0,
        "3B C6"                 // cmp     eax, esi
        "74 30"                 // jz      short loc_771CACAC
        "8B 85 6C FF FF FF"     // mov     eax, [ebp+var_94]
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
    int unk1;        // observed to be 0
    int dataLen;
    int bytesReceived1;
    int bytesReceived2;
    int padding2[9];
    int unk4;        // observed to be 0
} CFsm_SecureReceive_Upper;

typedef struct {
    char *data;
    int dataLen;
} CFsm_SecureSend_Upper;

static void *g_ICSecureSocketVTable = NULL;

static unsigned int g_icsocketBaseSize = 0;
static unsigned int g_cfsmSecureRecvBaseSize = 0;
static unsigned int g_cfsmSecureSendBaseSize = 0;
static void *g_secureRecvAfterDecryptReturnTrampoline = NULL;
static void *g_secureSendAfterEncryptReturnTrampoline = NULL;

static void *g_icasyncthreadMygetaddrRetAddr = NULL;

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

        mov        ecx, ebp; // store parent's ebp

        mov        ebp, esp;
        sub        esp, __LOCAL_SIZE;

        mov        [parent_ebp], ecx;
        mov        [self], edi;
        mov        [fsm], esi;
        mov        [retval], eax;
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
        mov        esp, ebp;

        popad;

        jmp [g_secureRecvAfterDecryptReturnTrampoline];
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

        mov        edi, ebp; // store parent's ebp

        mov        ebp, esp;
        sub        esp, __LOCAL_SIZE;

        mov        [parent_ebp], edi;
        mov        [fsm], esi;
        mov        [retval], eax;
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
        mov        esp, ebp;

        popad;

        jmp [g_secureSendAfterEncryptReturnTrampoline];
    }
}

static bool
getaddrinfoFromMyGetaddrShouldLog(CpuContext *context, va_list args)
{
    void *retAddr = *((void **) (context->ebp + 4));
    
    return (retAddr != g_icasyncthreadMygetaddrRetAddr);
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

    const FunctionSignature *signatures = signatures_ie8;
    g_icsocketBaseSize = 0x1c;
    g_cfsmSecureRecvBaseSize = 0x94;
    g_cfsmSecureSendBaseSize = 0x78;

    char *error;
    void *tmp;
    bool found = find_signature(&signatures[SIGNATURE_SECURE_RECV_AFTER_DECRYPT],
                                &tmp, &error);
    if (!found)
    {
        signatures = signatures_ie7;
    }

    void *retAddr;
    void **vtableAddr;

    found = find_signature(&signatures[SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET],
                           (LPVOID *) &vtableAddr, &error);
    if (!found)
    {
        sspy_free(error);

        signatures = signatures_ie6;
        g_icsocketBaseSize -= 4;
        g_cfsmSecureRecvBaseSize -= 4;
        g_cfsmSecureSendBaseSize -= 4;

        found = find_signature(&signatures[SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET],
                               (LPVOID *) &vtableAddr, &error);
    }

    if (!found)
    {
        LOG_OVERRIDE_ERROR("SIGNATURE_ICSECURESOCKET_VTABLE_OFFSET", error);
        return;
    }

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

    if (!intercept_code_matching(&signatures[SIGNATURE_SECURE_RECV_AFTER_DECRYPT],
                                 SecureReceiveAfterDecrypt, &g_secureRecvAfterDecryptReturnTrampoline,
                                 &error))
    {
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_RECV_AFTER_DECRYPT", error);
    }

    if (!intercept_code_matching(&signatures[SIGNATURE_SECURE_SEND_AFTER_ENCRYPT],
                                 SecureSendAfterEncrypt, &g_secureSendAfterEncryptReturnTrampoline,
                                 &error))
    {
        LOG_OVERRIDE_ERROR("SIGNATURE_SECURE_SEND_AFTER_ENCRYPT", error);
    }

    if (signatures == signatures_ie7)
    {
        if (find_signature(&signatures[SIGNATURE_ICASYNCTHREAD_MY_GETADDR],
                           &g_icasyncthreadMygetaddrRetAddr, &error))
        {
            if (find_signature(&signatures[SIGNATURE_MY_GETADDR_GETADDRINFO],
                               &retAddr, &error))
            {
                g_getaddrinfoHookContext.RegisterReturnAddress(retAddr, getaddrinfoFromMyGetaddrShouldLog);
            }
            else
            {
                LOG_OVERRIDE_ERROR("SIGNATURE_MY_GETADDR_GETADDRINFO", error);
            }
        }
        else
        {
            LOG_OVERRIDE_ERROR("SIGNATURE_ICASYNCTHREAD_MY_GETADDR", error);
        }
    }

    if (find_signature(&signatures[SIGNATURE_ICASYNCTHREAD_CONNECT],
                       &retAddr, &error))
    {
        g_connectHookContext.RegisterReturnAddress(retAddr);
    }
    else
    {
        LOG_OVERRIDE_ERROR("SIGNATURE_ICASYNCTHREAD_CONNECT", error);
    }
}

#pragma managed(pop)
