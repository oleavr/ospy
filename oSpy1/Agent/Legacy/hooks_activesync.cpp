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
#include <psapi.h>

#pragma managed(push, off)

//
// Signatures
//

typedef enum {
    SIGNATURE_UI_STATUS_LABEL_SET = 0,
    SIGNATURE_WIZ_STATUS_LABEL_SET,
    SIGNATURE_HTTP_REQUEST_FETCHED,
    SIGNATURE_ACTIVESYNC_DEBUG,
    SIGNATURE_WCESMGR_DEBUG2,
    SIGNATURE_WCESMGR_DEBUG2_ALTERNATE,
    SIGNATURE_WCESMGR_DEBUG3,
    SIGNATURE_WCESMGR_DEBUG3_ALTERNATE,
    SIGNATURE_WCESCOMM_DEBUG2,
    SIGNATURE_WCESCOMM_DEBUG2_ALTERNATE,
    SIGNATURE_WCESCOMM_DEBUG3,
    SIGNATURE_WCESCOMM_DEBUG3_ALTERNATE,
};

static FunctionSignature as_signatures[] = {
    // SIGNATURE_UI_STATUS_LABEL_SET
    {
        "wcesmgr.exe",
		0,
        "56"                    // push    esi
        "FF 74 24 08"           // push    [esp+text]
        "8B F1"                 // mov     esi, ecx
        "E8 ?? ?? ?? 00"        // call    ?SetWindowTextA@CWnd@@QAEXPBD@Z ; CWnd::SetWindowTextA(char const *)
        "68 C8 00 00 00"        // push    200             ; iMaxLength
        "FF 74 24 0C"           // push    dword ptr [esp+4+text] ; lpString2
        "8D 46 54"              // lea     eax, [esi+54h]
        "50"                    // push    eax             ; lpString1
        "FF 15 ?? ?? ?? 00"     // call    ds:lstrcpynA
        "6A 01"                 // push    1               ; bErase
        "6A 00"                 // push    0               ; lpRect
        "FF 76 20"              // push    dword ptr [esi+20h] ; hWnd
        "FF 15 ?? ?? ?? 00"     // call    ds:InvalidateRect
        "FF 76 20"              // push    dword ptr [esi+20h] ; hWnd
        "FF 15 ?? ?? ?? 00"     // call    ds:UpdateWindow
        "5E"                    // pop     esi
        "C2 04 00",             // retn    4
    },

    // SIGNATURE_WIZ_STATUS_LABEL_SET
    {
        "wcesmgr.exe",
		0,
        "83 C1 74"              // add     ecx, 74h
        "51"                    // push    ecx
        "68 27 02 00 00"        // push    227h
        "FF 74 24 0C"           // push    [esp+8+pDX]
        "E8 ?? ?? ?? 00"        // call    ?DDX_Text@@YGXPAVCDataExchange@@HAAV?$CStringT@DV?$StrTraitMFC_DLL@DV?$ChTraitsCRT@D@ATL@@@@@ATL@@@Z ; DDX_Text(CDataExchange *,int,ATL::CStringT<char,StrTraitMFC_DLL<char,ATL::ChTraitsCRT<char>>> &)
        "C2 04 00",             // retn    4
    },

    // SIGNATURE_HTTP_REQUEST_FETCHED
    {
        "httpsys.dll",
		0,
        "8B 06"                 // mov     eax, [esi]
        "8B CE"                 // mov     ecx, esi
        "FF 10"                 // call    dword ptr [eax]
        "89 45 FC"              // mov     [ebp+var_4], eax
    },

    //
    // SIGNATURE_ACTIVESYNC_DEBUG
    //
    // used by wcesmgr.exe, rapimgr.exe, wcescomm.exe, rapistub.dll
    // and possibly more...
    //
    {
        "<NULL>",
		0,
        "8B 44 24 08"           // mov     eax, [esp+arg_4]
        "57"                    // push    edi
        "33 FF"                 // xor     edi, edi
        "85 C0"                 // test    eax, eax
        "74 5D"                 // jz      short loc_59375D
        "66 39 38"              // cmp     [eax], di
        "74 58",                // jz      short loc_59375D
    },

    // SIGNATURE_WCESMGR_DEBUG2
    {
        "wcesmgr.exe",
		0,
        "55"                    // push    ebp
        "8B EC"                 // mov     ebp, esp
        "8B 45 08"              // mov     eax, [ebp+arg_0]
        "56"                    // push    esi
        "57"                    // push    edi
        "8D B0 10 01 00 00"     // lea     esi, [eax+110h]
        "33 FF",                // xor     edi, edi
    },

	// SIGNATURE_WCESMGR_DEBUG2_ALTERNATE
    {
        "wcesmgr.exe",
		0,
		"55"					// push    ebp
		"8B EC"					// mov     ebp, esp
		"A1 ?? ?? ?? ??"		// mov     eax, dword_5C6668
		"53"					// push    ebx
		"33 DB"					// xor     ebx, ebx
		"85 45 0C"				// test    [ebp+arg_4], eax
		"74 51"					// jz      short loc_57FF18
    },

    // SIGNATURE_WCESMGR_DEBUG3
    {
        "wcesmgr.exe",
		0,
        "55"                    // push    ebp
        "8B EC"                 // mov     ebp, esp
        "8B 45 08"              // mov     eax, [ebp+arg_0]
        "57"                    // push    edi
        "33 FF"                 // xor     edi, edi
        "39 B8 08 01 00 00"     // cmp     [eax+108h], edi
    },

	// SIGNATURE_WCESMGR_DEBUG3_ALTERNATE
    {
        "wcesmgr.exe",
		0,
        "55"                    // push    ebp
        "8B EC"                 // mov     ebp, esp
        "8B 45 08"              // mov     eax, [ebp+arg_0]
        "53"                    // push    ebx
        "33 DB"					// xor     ebx, ebx
        "39 98 08 01 00 00"		// cmp     [eax+108h], ebx
    },

    // SIGNATURE_WCESCOMM_DEBUG2
    {
        "wcescomm.exe",
		0,
        "55"                    // push    ebp
        "8D AC 24 6C FC FF FF"  // lea     ebp, [esp-394h]
        "81 EC 14 04 00 00"     // sub     esp, 414h
        "A1 ?? ?? ?? 00"        // mov     eax, dword_42E540
        "57"                    // push    edi
        "89 85 90 03 00 00"     // mov     [ebp+394h+var_4], eax
        "33 C0"                 // xor     eax, eax
        "83 3D ?? ?? ?? 00 00"  // cmp     dword_42EAA8, 0
        "C6 45 90 00"           // mov     [ebp+394h+var_404], 0
        "B9 FF 00 00 00"        // mov     ecx, 0FFh
        "8D 7D 91"              // lea     edi, [ebp-6Fh]
        "F3 AB"                 // rep stosd
        "66 AB"                 // stosw
        "AA"                    // stosb
        "0F 84 CE 00 00 00"     // jz      loc_40ECCC
    },

    // SIGNATURE_WCESCOMM_DEBUG2_ALTERNATE
    {
        "wcescomm.exe",
		50,
		"83 3D ?? ?? ?? ?? 00"  // cmp     dword_430BB4, 0
		"0F 84 D0 00 00 00"		// jz      loc_410BB4
	},

    // SIGNATURE_WCESCOMM_DEBUG3
    {
        "wcescomm.exe",
		0,
        "55"                    // push    ebp
        "8D AC 24 6C FC FF FF"  // lea     ebp, [esp-394h]
        "81 EC 14 04 00 00"     // sub     esp, 414h
        "A1 ?? ?? ?? 00"        // mov     eax, dword_42E540
        "57"                    // push    edi
        "89 85 90 03 00 00"     // mov     [ebp+394h+var_4], eax
        "33 C0"                 // xor     eax, eax
        "83 3D ?? ?? ?? 00 00"  // cmp     dword_42EAA8, 0
        "C6 45 90 00"           // mov     [ebp+394h+var_404], 0
        "B9 FF 00 00 00"        // mov     ecx, 0FFh
        "8D 7D 91"              // lea     edi, [ebp-6Fh]
        "F3 AB"                 // rep stosd
        "66 AB"                 // stosw
        "AA"                    // stosb
        "0F 84 E1 00 00 00"     // jz      loc_40EDFB
    },

	// SIGNATURE_WCESCOMM_DEBUG3_ALTERNATE
    {
        "wcescomm.exe",
		50,
		"83 3D ?? ?? ?? ?? 00"	// cmp     dword_430BB4, 0
		"0F 84 E3 00 00 00"		// jz      loc_410CEB
	},
};


//
// WCESMgr.exe
//

static LPVOID http_request_fetched_impl = NULL;

static __declspec(naked) void
http_request_fetched(LPWSTR context,
                     HTTP_REQUEST *request,
                     char *body,
                     int body_size,
                     HANDLE req_queue_handle)
{
    void *self;

    __asm {
        /* do what the original function did at the point where we overwrote it */
        push            eax;   /* dummy padding to align argument offsets */
                               /* (since this function isn't called but jumped directly to) */
        push	        ebp;
        mov		ebp, esp;
        sub		esp, __LOCAL_SIZE;
        pushad;
        mov		[self], esi;
    }

    log_http_request(req_queue_handle, request, body, body_size);

    __asm {
        popad;
        mov		esp, ebp;
        pop		ebp;

        pop             eax; /* remove dummy padding */
        
        /* continue where the original left off */
        mov             ecx, esi;

        /* simulate call from the original location */
        mov             eax, [http_request_fetched_impl]
        lea             eax, [eax+6]
        push            eax

        mov             eax, [esi];     /* vtable */
        jmp             dword ptr [eax] /* vtable[0] */
    }
}

static ULONG __cdecl
HttpSendHttpResponse_called(BOOL carry_on,
                            DWORD ret_addr,
                            HANDLE ReqQueueHandle,
                            HTTP_REQUEST_ID RequestId,
                            ULONG Flags,
                            PHTTP_RESPONSE pHttpResponse,
                            PVOID pReserved1,
                            PULONG pBytesSent,
                            PVOID pReserved2,
                            ULONG Reserved3,
                            LPOVERLAPPED pOverlapped,
                            PVOID pReserved4)
{
    /* ignored as long as carry_on isn't touched */
    return 0;
}

static ULONG __stdcall
HttpSendHttpResponse_done(ULONG retval,
                          HANDLE ReqQueueHandle,
                          HTTP_REQUEST_ID RequestId,
                          ULONG Flags,
                          PHTTP_RESPONSE pHttpResponse,
                          PVOID pReserved1,
                          PULONG pBytesSent,
                          PVOID pReserved2,
                          ULONG Reserved3,
                          LPOVERLAPPED pOverlapped,
                          PVOID pReserved4)
{
    DWORD err = GetLastError();

    if (retval == NO_ERROR)
    {
        log_http_response(ReqQueueHandle, RequestId, pHttpResponse, NULL, 0);
    }

    SetLastError(err);
    return retval;
}

static ULONG __cdecl
HttpSendResponseEntityBody_called(BOOL carry_on,
                                  DWORD ret_addr,
                                  HANDLE ReqQueueHandle,
                                  HTTP_REQUEST_ID RequestId,
                                  ULONG Flags,
                                  USHORT EntityChunkCount,
                                  PHTTP_DATA_CHUNK pEntityChunks,
                                  PULONG pBytesSent,
                                  PVOID pReserved1,
                                  ULONG Reserved2,
                                  LPOVERLAPPED pOverlapped,
                                  PVOID pReserved3)
{
    /* ignored as long as carry_on isn't touched */
    return 0;
}

static ULONG __cdecl
HttpSendResponseEntityBody_done(ULONG retval,
                                HANDLE ReqQueueHandle,
                                HTTP_REQUEST_ID RequestId,
                                ULONG Flags,
                                USHORT EntityChunkCount,
                                PHTTP_DATA_CHUNK pEntityChunks,
                                PULONG pBytesSent,
                                PVOID pReserved1,
                                ULONG Reserved2,
                                LPOVERLAPPED pOverlapped,
                                PVOID pReserved3)
{
    DWORD err = GetLastError();

    if (retval == NO_ERROR)
    {
        if (EntityChunkCount > 0)
        {
            HTTP_DATA_CHUNK *chunk = &pEntityChunks[0];

            if (chunk->DataChunkType == HttpDataChunkFromMemory)
            {
                log_http_response(ReqQueueHandle, RequestId, NULL,
                   (const char *) chunk->FromMemory.pBuffer,
                                  chunk->FromMemory.BufferLength);
            }
            else
            {
                message_logger_log_message("HttpSendResponseEntityBody", NULL,
                    MESSAGE_CTX_ERROR,
                    "only HttpDataChunkFromMemory is supported for now");
            }

            if (EntityChunkCount > 1)
            {
                message_logger_log_message("HttpSendResponseEntityBody", NULL,
                    MESSAGE_CTX_WARNING,
                    "only EntityChunkCount == 1 is supported for now");
            }
        }
    }

    SetLastError(err);
    return retval;
}

/* 9 DWORD arguments and 1 QWORD argument */
HOOK_GLUE_INTERRUPTIBLE(HttpSendHttpResponse, ((9 * 4) + (1 * 8)))
HOOK_GLUE_INTERRUPTIBLE(HttpSendResponseEntityBody, ((9 * 4) + (1 * 8)))

typedef struct {
	int padding[8];
	HWND m_hWnd;
} CWnd;

static __declspec(naked) void
ui_status_label_set(char *text)
{
    CWnd *self;
    char *p;
    RECT parent_rect, label_rect;
    int x, y;

    __asm {
        push	ebp;
        mov		ebp, esp;
        sub		esp, __LOCAL_SIZE;
        pushad;
        mov		[self], ecx;
    }

	GetWindowRect(GetParent(self->m_hWnd), &parent_rect);
	GetWindowRect(self->m_hWnd, &label_rect);

    x = label_rect.left - parent_rect.left;
    y = label_rect.top - parent_rect.top;

    if (y >= 40)
    {
        /* substatus: y = 50 */
        message_logger_log_message("UIStatusLabelSet", NULL,
            MESSAGE_CTX_ACTIVESYNC_SUBSTATUS,
            text);
    }
    else if (y >= 20)
    {
        /* status: y = 33 */
        message_logger_log_message("UIStatusLabelSet", NULL,
            MESSAGE_CTX_ACTIVESYNC_STATUS,
            text);
    }
    else
    {
        /* device: y = 2 */
        message_logger_log_message("UIStatusLabelSet", NULL,
            MESSAGE_CTX_ACTIVESYNC_DEVICE,
            text);
    }

	SetWindowTextA(self->m_hWnd, text);

    p = (char *) self + 0x54;
    lstrcpynA(p, text, 200);

    InvalidateRect(self->m_hWnd, NULL, TRUE);
    UpdateWindow(self->m_hWnd);

    __asm {
        popad;
        mov		esp, ebp;
        pop		ebp;
        retn	4;
    }
}

static LPVOID wiz_status_label_set_impl = NULL;

static __declspec(naked) void
wiz_status_label_set(void *pDX /* CDataExchange * */ )
{
    char *status;

    __asm {
        push	        ebp;
        mov		ebp, esp;
        sub		esp, __LOCAL_SIZE;
        pushad;
        add		ecx, 74h
        mov		ecx, [ecx]
        mov		[status], ecx
    }

    message_logger_log_message("WizStatusLabelSet", NULL,
        MESSAGE_CTX_ACTIVESYNC_WZ_STATUS,
        status);

    __asm {
        popad;
        mov		esp, ebp;
        pop		ebp;

        // Do what the original function did where we overwrote it,
        // and jump directly to the next instruction.
        add             ecx, 74h    // 83 C1 74
        push            ecx         // 51
        push            227h        // 68 27 02 00 00

        mov             eax, [wiz_status_label_set_impl]
        lea             eax, [eax+9]
        jmp		eax
    }
}

static void __cdecl
wcesmgr_debug_1(void *obj,
                const LPWSTR format,
                ...)
{
    va_list args;

    va_start(args, format);
    log_debug_w("WCESMgrDebug", (char *) &obj - 4, format, args);
}

static void __cdecl
wcesmgr_debug_2(void *obj,
                int level,
                char *format,
                ...)
{
    va_list args;

    va_start(args, format);
    log_debug("WCESMgrDebug2", (char *) &obj - 4, format, args);
}

static void __cdecl
wcesmgr_debug_3(void *obj,
                char *format,
                ...)
{
    va_list args;

    va_start(args, format);
    log_debug("WCESMgrDebug3", (char *) &obj - 4, format, args);
}


//
// rapimgr.exe
//

static void __cdecl
rapimgr_debug(void *obj,
              const LPWSTR format,
              ...)
{
    va_list args;

    va_start(args, format);
    log_debug_w("RAPIMgrDebug", (char *) &obj - 4, format, args);
}


//
// wcescomm.exe
//

static void __cdecl
wcescomm_debug_1(void *obj,
                 const LPWSTR format,
                 ...)
{
    va_list args;

    va_start(args, format);
    log_debug_w("WCESCommDebug1", (char *) &obj - 4, format, args);
}

static void __cdecl
wcescomm_debug_2(const char *format,
                 ...)
{
    va_list args;

    va_start(args, format);
    log_debug("WCESCommDebug2", (char *) &format - 4, format, args);
}

static void __cdecl
wcescomm_debug_3(const char *format,
                 ...)
{
    va_list args;

    va_start(args, format);
    log_debug("WCESCommDebug3", (char *) &format - 4, format, args);
}


//
// rapistub.dll
//

static void __cdecl
rapistub_debug(void *obj,
               const LPWSTR format,
               ...)
{
    va_list args;

    va_start(args, format);
    log_debug_w("RAPIStubDebug", (char *) &obj - 4, format, args);
}


#define LOG_OVERRIDE_ERROR(sig, e) \
            message_logger_log_message("hook_activesync", 0, MESSAGE_CTX_ERROR,\
                "override_function_by_signature for " sig " failed: %s", e);\
            sspy_free(e)

void
hook_activesync()
{
    HKEY key = 0;
    DWORD val_type;
    WCHAR path[_MAX_PATH];
    DWORD path_len = sizeof(path);
    WCHAR drive[_MAX_DRIVE];
    WCHAR dir[_MAX_DIR];
    WCHAR fname[_MAX_FNAME];
    WCHAR ext[_MAX_EXT];
    char *error;
    TCHAR tmp_path[_MAX_PATH];
    HMODULE h;

    _httpapi_util_init();

    if (RegOpenKeyExW(HKEY_CLASSES_ROOT,
        L"CLSID\\{ED081F25-6A77-4C89-B689-C6E15C582EC1}\\LocalServer32", /* rapimgr.exe */
        0, KEY_READ, &key) != ERROR_SUCCESS)
    {
        goto ACTIVESYNC_NOT_FOUND;
    }

    if (RegQueryValueExW(key, NULL, /* the default value */
        NULL, &val_type, (LPBYTE) path, &path_len) != ERROR_SUCCESS)
    {
        goto ACTIVESYNC_NOT_FOUND;
    }

    if (val_type != REG_SZ)
    {
        goto ACTIVESYNC_NOT_FOUND;
    }

    _wsplitpath_s(path, drive, _MAX_DRIVE, dir, _MAX_DIR,
        fname, _MAX_FNAME, ext, _MAX_EXT);

    if (cur_process_is("wcesmgr.exe"))
    {
        // UI status labels
        if (!override_function_by_signature(&as_signatures[SIGNATURE_UI_STATUS_LABEL_SET],
                                            ui_status_label_set, NULL, &error))
        {
            LOG_OVERRIDE_ERROR("SIGNATURE_UI_STATUS_LABEL_SET", error);
        }

        // Wizard status label
        if (!override_function_by_signature(&as_signatures[SIGNATURE_WIZ_STATUS_LABEL_SET],
                                            wiz_status_label_set, &wiz_status_label_set_impl,
                                            &error))
        {
            LOG_OVERRIDE_ERROR("SIGNATURE_WIZ_STATUS_LABEL_SET", error);
        }

        // Hook httpsys.dll for catching incoming requests (since httpapi.dll
        // is used asynchronously for this so this is the quickest path) */
        wsprintf(tmp_path, "%S%S%S", drive, dir, L"httpsys.dll");

        h = LoadLibrary(tmp_path);
        if (h != NULL)
        {
            if (!override_function_by_signature(&as_signatures[SIGNATURE_HTTP_REQUEST_FETCHED],
                                                http_request_fetched, &http_request_fetched_impl,
                                                &error))
            {
                LOG_OVERRIDE_ERROR("SIGNATURE_HTTP_REQUEST_FETCHED", error);
            }
        }
        else
        {
            message_logger_log_message("DllMain", 0, MESSAGE_CTX_ERROR,
                "ActiveSync found but loading '%s' failed",
                tmp_path);
        }

        // Hook httpapi.dll for outgoing responses (synchronous only,
        // which is fine for ActiveSync)
        h = LoadLibrary("httpapi.dll");
        if (h != NULL)
        {
            HOOK_FUNCTION(h, HttpSendHttpResponse);
            HOOK_FUNCTION(h, HttpSendResponseEntityBody);
        }
        else
        {
            message_logger_log_message("DllMain", 0, MESSAGE_CTX_ERROR,
                "Failed to load httpapi.dll");
        }

        // Hook the internal debug/logging functions
        if (!override_function_by_signature_in_module(
            &as_signatures[SIGNATURE_ACTIVESYNC_DEBUG], "wcesmgr.exe",
            wcesmgr_debug_1, NULL, &error))
        {
            LOG_OVERRIDE_ERROR("SIGNATURE_ACTIVESYNC_DEBUG", error);
        }

        if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESMGR_DEBUG2],
                                            wcesmgr_debug_2, NULL, &error))
        {
			sspy_free(error);

			if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESMGR_DEBUG2_ALTERNATE],
												wcesmgr_debug_2, NULL, &error))
			{
	            LOG_OVERRIDE_ERROR("SIGNATURE_WCESMGR_DEBUG2", error);
			}
        }

        if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESMGR_DEBUG3],
                                            wcesmgr_debug_3, NULL, &error))
        {
			sspy_free(error);

			if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESMGR_DEBUG3_ALTERNATE],
												wcesmgr_debug_3, NULL, &error))
			{
	            LOG_OVERRIDE_ERROR("SIGNATURE_WCESMGR_DEBUG3", error);
			}
        }
    }
    else if (cur_process_is("rapimgr.exe"))
    {
        // Hook the internal debug function
        if (!override_function_by_signature_in_module(
            &as_signatures[SIGNATURE_ACTIVESYNC_DEBUG], "rapimgr.exe",
            rapimgr_debug, NULL, &error))
        {
            LOG_OVERRIDE_ERROR("SIGNATURE_ACTIVESYNC_DEBUG", error);
        }
    }
    else if (cur_process_is("wcescomm.exe"))
    {
        // Hook the internal debug/logging functions
        if (!override_function_by_signature_in_module(
            &as_signatures[SIGNATURE_ACTIVESYNC_DEBUG], "wcescomm.exe",
            wcescomm_debug_1, NULL, &error))
        {
            LOG_OVERRIDE_ERROR("SIGNATURE_ACTIVESYNC_DEBUG", error);
        }

        if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESCOMM_DEBUG2],
                                            wcescomm_debug_2, NULL, &error))
        {
			sspy_free(error);

			if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESCOMM_DEBUG2_ALTERNATE],
												wcescomm_debug_2, NULL, &error))
			{
	            LOG_OVERRIDE_ERROR("SIGNATURE_WCESCOMM_DEBUG2", error);
			}
        }

        if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESCOMM_DEBUG3],
                                            wcescomm_debug_3, NULL, &error))
        {
			sspy_free(error);

			if (!override_function_by_signature(&as_signatures[SIGNATURE_WCESCOMM_DEBUG3_ALTERNATE],
												wcescomm_debug_3, NULL, &error))
			{
	            LOG_OVERRIDE_ERROR("SIGNATURE_WCESCOMM_DEBUG3", error);
			}
        }
    }

    /* Hook rapistub.dll for all processes */
    wsprintf(tmp_path, "%S%S%S", drive, dir, L"rapistub.dll");

    h = LoadLibrary(tmp_path);
    if (h != NULL)
    {
        // Hook the internal debug function
        if (!override_function_by_signature_in_module(
            &as_signatures[SIGNATURE_ACTIVESYNC_DEBUG], "rapistub.dll",
            rapistub_debug, NULL, &error))
        {
            LOG_OVERRIDE_ERROR("SIGNATURE_ACTIVESYNC_DEBUG", error);
        }
    }
    else
    {
        message_logger_log_message("DllMain", 0, MESSAGE_CTX_ERROR,
            "ActiveSync found but loading '%s' failed",
            tmp_path);
    }

    goto DONE;

ACTIVESYNC_NOT_FOUND:
    message_logger_log_message("DllMain", 0, MESSAGE_CTX_INFO,
        "ActiveSync not found, API not hooked");

DONE:
    if (key != 0)
    {
        RegCloseKey(key);
    }
}

#pragma managed(pop)