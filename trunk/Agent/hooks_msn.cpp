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
    SIGNATURE_IDCRL_DEBUG = 0,
};

static FunctionSignature msn_signatures[] = {
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

void
hook_msn()
{
    //char *error;

    if (!cur_process_is("msnmsgr.exe"))
        return;

    // IDCRL internal debugging function
    /*
    if (!override_function_by_signature(&msn_signatures[SIGNATURE_IDCRL_DEBUG],
                                        idcrl_debug, NULL, &error))
    {
        LOG_OVERRIDE_ERROR(error);
    }
    */
}
