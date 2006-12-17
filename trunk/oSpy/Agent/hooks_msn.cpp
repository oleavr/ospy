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

//
// Signatures
//

typedef enum {
    SIGNATURE_GET_CHALLENGE_SECRET = 0,
    SIGNATURE_IDCRL_DEBUG          = 1,
};

static FunctionSignature msn_signatures[] = {
    // SIGNATURE_GET_CHALLENGE_SECRET
    {
        "msnmsgr.exe",

        "6A 08"                 // push    8
        "B8 ?? ?? ?? ??"        // mov     eax, 846D06h
        "E8 ?? ?? ?? ??"        // call    __EH_prolog3
        "83 65 F0 00"           // and     dword ptr [ebp-10h], 0
        "80 7D 0C 00"           // cmp     byte ptr [ebp+0Ch], 0
        "68 B7 00 00 00"        // push    183
        "74 BF"                 // jz      short loc_4F8319
        "E8 ?? ?? ?? ??"        // call    loc_538EF6
    },

    // SIGNATURE_IDCRL_DEBUG
    {
        "msidcrl40.dll",

        "55"                    // push    ebp
        "8B EC"                 // mov     ebp, esp
        "81 EC 08 08 00 00"     // sub     esp, 808h
        "A1 ?? ?? ?? ??"        // mov     eax, dword_275BB21C
        "33 C5"                 // xor     eax, ebp
        "89 45 FC"              // mov     [ebp+var_4], eax
        "8B 45 10"              // mov     eax, [ebp+arg_8]
        "8A 4D 0C"              // mov     cl, [ebp+arg_4]
        "8B 55 14"              // mov     edx, [ebp+arg_C]
        "53"                    // push    ebx
        "8B 5D 18"              // mov     ebx, [ebp+arg_10]
        "57"                    // push    edi
        "8B 7D 08"              // mov     edi, [ebp+arg_0]
        "89 85 F8 F7 FF FF"     // mov     [ebp+var_808], eax
        "C7 07 ?? ?? 50 27"     // mov     dword ptr [edi], offset off_27503070
    },
};

static void __cdecl
idcrl_debug(void *obj,
            int msg_type,
            LPWSTR filename,
            int *something,
            LPWSTR function_name,
            LPWSTR message,
            ...)
{
    DWORD ret_addr = *((DWORD *) ((DWORD) &obj - 4));
    va_list args;
    size_t size;
    LPWSTR buf;

    size = sizeof(WCHAR); // space for NUL termination
    if (function_name != NULL)
        size += wcslen(function_name) * sizeof(WCHAR);
    if (message != NULL)
    {
        if (function_name != NULL)
            size += 2 * sizeof(WCHAR);

        size += wcslen(message) * sizeof(WCHAR);
    }

    if (size <= sizeof(WCHAR))
        return;

    buf = (LPWSTR) sspy_malloc(size);
    wsprintfW(buf, L"%s%s%s",
        (function_name != NULL) ? function_name : L"",
        (function_name != NULL && message != NULL) ? L": " : L"",
        (message != NULL) ? message : L"");

    va_start(args, message);
    log_debug_w("MSNIDCRL", ret_addr, buf, args);

    sspy_free(buf);
}

#define LOG_OVERRIDE_ERROR(e) \
            message_logger_log_message("hook_msn", 0, MESSAGE_CTX_ERROR,\
                "override_function_by_signature failed: %s", e);\
            sspy_free(e)

typedef const char *(__stdcall *GetChallengeSecretFunc) (const char **ret, int which_one);

void
hook_msn()
{
    char *error;

    if (!cur_process_is("msnmsgr.exe"))
        return;

    GetChallengeSecretFunc get_challenge_secret;

    if (find_signature(&msn_signatures[SIGNATURE_GET_CHALLENGE_SECRET],
        (LPVOID *) &get_challenge_secret, &error))
    {
        ByteBuffer *buf = byte_buffer_sized_new(64);
        const char *product_id, *product_key;

        get_challenge_secret(&product_id, 1);
        get_challenge_secret(&product_key, 0);

        byte_buffer_append_printf(buf, "Product ID: '");
        byte_buffer_append(buf, (void *) product_id, strlen(product_id));
        byte_buffer_append_printf(buf, "'\r\n");

        byte_buffer_append_printf(buf, "Product Key: '");
        byte_buffer_append(buf, (void *) product_key, strlen(product_key));
        byte_buffer_append_printf(buf, "'");

        message_logger_log("hook_msn", 0, 0, MESSAGE_TYPE_PACKET,
            MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID, NULL, NULL,
            (const char *) buf->buf, (int) buf->offset, "Product ID and Key");

        byte_buffer_free(buf);
    }
    else
    {
        message_logger_log_message("hook_msn", 0, MESSAGE_CTX_WARNING,
            "failed to find SIGNATURE_GET_CHALLENGE_SECRET: %s", error);
        sspy_free(error);
    }

    // IDCRL internal debugging function
    /*
    if (!override_function_by_signature(&msn_signatures[SIGNATURE_IDCRL_DEBUG],
                                        idcrl_debug, NULL, &error))
    {
        LOG_OVERRIDE_ERROR(error);
    }
    */
}
