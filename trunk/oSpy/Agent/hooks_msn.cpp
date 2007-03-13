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
#include "vtable.h"

//
// Signatures
//

typedef enum {
    SIGNATURE_GET_CHALLENGE_SECRET = 0,
	SIGNATURE_MSNMSGR_DEBUG,
    SIGNATURE_IDCRL_DEBUG,
	SIGNATURE_CONTACT_PROPERTY_ID_TO_NAME,
	SIGNATURE_CONTACT_PROPERTY_ID_TO_NAME_2,
};

static FunctionSignature msn_signatures[] = {
    // SIGNATURE_GET_CHALLENGE_SECRET
    {
        "msnmsgr.exe",
		16,
        "80 7D 0C 00"           // cmp     byte ptr [ebp+0Ch], 0
        "68 B7 00 00 00"        // push    183
    },

	// SIGNATURE_MSNMSGR_DEBUG
	{
		"msnmsgr.exe",
		11,
		"84 C0"					// test    al, al
		"74 14"					// jz      short loc_448B51
		"83 7D 0C 02"			// cmp     [ebp+arg_4], 2
		"77 0E"					// ja      short loc_448B51
		"FF 75 14"				// push    [ebp+arg_C]     ; va_list
		"FF 75 10"				// push    [ebp+arg_8]     ; wchar_t *
		"FF 75 08"				// push    [ebp+arg_0]     ; int
		"E8"					// call    ...
	},

    // SIGNATURE_IDCRL_DEBUG
    {
        "msidcrl40.dll",
		0,
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

	// SIGNATURE_CONTACT_PROPERTY_ID_TO_NAME
	{
		"msnmsgr.exe",
		-4,						// we include the last two instructions of the function before
								// the one we're interested in in order to make sure we find
								// just one match (since this function is so generic)

		"5D"					// pop     ebp
		"C2 0C 00"				// retn    0Ch

		"55"					// push    ebp
		"8B EC"					// mov     ebp, esp
		"8B 45 08"				// mov     eax, [ebp+arg_0]
		"83 F8 ??"				// cmp     eax, 71         ; switch 72 cases
		"0F 87 ?? ?? ?? ??"		// ja      loc_479F00      ; default
		"FF 24 85 ?? ?? ?? ??"	// jmp     ds:off_46D134[eax*4] ; switch jump
	},

	// SIGNATURE_CONTACT_PROPERTY_ID_TO_NAME_2
	{
		"msnmsgr.exe",
		-6,						// we include the last two instructions of the function before
								// the one we're interested in in order to make sure we find
								// just one match (since this function is so generic)

		"5E"					// pop     esi
		"E9 ?? ?? ?? ??"		// jmp     sub_421B16

		"55"					// push    ebp
		"8B EC"					// mov     ebp, esp
		"8B 45 08"				// mov     eax, [ebp+arg_0]
		"83 F8 ??"				// cmp     eax, 71         ; switch 72 cases
		"0F 87 ?? ?? ?? ??"		// ja      loc_479F00      ; default
		"FF 24 85 ?? ?? ?? ??"	// jmp     ds:off_46D134[eax*4] ; switch jump
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
    log_debug_w("MSNIDCRL", (char *) &obj - 4, buf, args);

    sspy_free(buf);
}

static void __stdcall
msnmsgr_debug(DWORD domain,
			  DWORD severity,
			  LPWSTR fmt_str,
			  va_list args)
{
    DWORD ret_addr = *((DWORD *) ((DWORD) &domain - 4));
	WCHAR bad_str[] = L"	spServiceOut = 0x%p {%ls, ls%}";
	WCHAR good_str[] = L"	spServiceOut = 0x%p {%ls, %ls}";

	if (memcmp(fmt_str, bad_str, sizeof(bad_str)) == 0)
		fmt_str = good_str;

	log_debug_w("MsnmsgrDebug", (char *) &domain - 4, fmt_str, args, domain, severity);
}

#define LOG_OVERRIDE_ERROR(e) \
            message_logger_log_message("hook_msn", 0, MESSAGE_CTX_ERROR,\
                "override_function_by_signature failed: %s", e);\
            sspy_free(e)

typedef const char *(__stdcall *GetChallengeSecretFunc) (const char **ret, int which_one);
typedef const LPWSTR (__stdcall *ContactPropertyIdToNameFunc) (int property_id);

void
hook_msn()
{
    char *error;

    if (!cur_process_is("msnmsgr.exe"))
        return;

#if 0
	VTableSpec *vtableSpec = new VTableSpec("CTCPTransportBridge", 22);
	VTableSpec &vts = *vtableSpec;
	vts[0].SetName("OnBridgePeerConnectingEndpointsUpdated");
	vts[1].SetName("OnBridgePeerListeningEndpointsUpdated");
	vts[4].SetName("P2PListen");
	vts[5].SetName("P2PConnect");
	vts[7].SetName("Destroy");
	vts[8].SetName("Shutdown");
	vts[9].SetName("Init");
	vts[10].SetName("Send");
	vts[11].SetName("GetProperties");
	vts[12].SetName("ReadyToSend");
	vts[13].SetName("ConnectBridge");
	vts[17].SetName("OnReceivedConnectRequest");
	vts[18].SetName("OnDataReceived");
	vts[19].SetName("OnDataResponse");
	vts[20].SetName("OnBridgeStateChange");

	VTableInstance *directVTable = new VTableInstance(vtableSpec, 0x484C64);
	VTableHooker::Self()->HookVTable(directVTable);
#endif

    GetChallengeSecretFunc get_challenge_secret;

    if (find_signature(&msn_signatures[SIGNATURE_GET_CHALLENGE_SECRET],
        (LPVOID *) &get_challenge_secret, &error))
    {
		OStringStream s;
        const char *product_id, *product_key;

        get_challenge_secret(&product_id, 1);
        get_challenge_secret(&product_key, 0);

		s << "Product ID: '" << product_id << "'\r\n";
		s << "Product Key: '" << product_key << "'";

        message_logger_log("hook_msn", 0, 0, MESSAGE_TYPE_PACKET,
            MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID, NULL, NULL,
			s.str().c_str(), s.str().size(), "Product ID and Key");
    }
    else
    {
        message_logger_log_message("hook_msn", 0, MESSAGE_CTX_WARNING,
            "failed to find SIGNATURE_GET_CHALLENGE_SECRET: %s", error);
        sspy_free(error);
    }

	if (!override_function_by_signature(&msn_signatures[SIGNATURE_MSNMSGR_DEBUG],
									    msnmsgr_debug, NULL, &error))
	{
		LOG_OVERRIDE_ERROR(error);
	}

	ContactPropertyIdToNameFunc contact_property_id_to_name;

	bool found = find_signature(&msn_signatures[SIGNATURE_CONTACT_PROPERTY_ID_TO_NAME],
								(LPVOID *) &contact_property_id_to_name, &error);
	if (!found)
	{
		sspy_free(error);
		found = find_signature(&msn_signatures[SIGNATURE_CONTACT_PROPERTY_ID_TO_NAME_2],
							   (LPVOID *) &contact_property_id_to_name, &error);
	}

    if (found)
    {
		OStringStream s;

		for (int i = 0; i < 1024; i++)
		{
			const LPWSTR name = contact_property_id_to_name(i);
			if (name == NULL)
				continue;

			if (i != 0)
				s << "\r\n";

			int bufSize = wcslen(name) + 1;
			char *buf = (char *) sspy_malloc(bufSize);

		    WideCharToMultiByte(CP_ACP, 0, name, -1, buf, bufSize, NULL, NULL);

			s << i << " => " << buf;

			sspy_free(buf);
		}

        message_logger_log("hook_msn", 0, 0, MESSAGE_TYPE_PACKET,
            MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID, NULL, NULL,
			s.str().c_str(), s.str().size(), "Contact property id mappings");
	}
    else
    {
        message_logger_log_message("hook_msn", 0, MESSAGE_CTX_WARNING,
            "failed to find SIGNATURE_CONTACT_PROPERTY_ID_TO_NAME: %s", error);
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
