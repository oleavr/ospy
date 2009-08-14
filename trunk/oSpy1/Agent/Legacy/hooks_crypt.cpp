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

/* Ugly, but we'd like access to all definitions regardless of OS version */
#define ORIGINAL_NTDDI_VERSION NTDDI_VERSION
#undef NTDDI_VERSION
#define NTDDI_VERSION NTDDI_LONGHORN
#include <wincrypt.h>
#undef NTDDI_VERSION
#define NTDDI_VERSION ORIGINAL_NTDDI_VERSION
#undef ORIGINAL_NTDDI_VERSION

#include <psapi.h>
#include <map>

#pragma managed(push, off)

static MODULEINFO schannel_info;
static MODULEINFO crypt32_info;

static LPVOID own_base;
static DWORD own_size;

#define CRYPT_ENCRYPT_ARGS_SIZE (7 * 4)
#define CRYPT_DECRYPT_ARGS_SIZE (6 * 4)

static BOOL
called_internally(DWORD ret_addr)
{
    if (ret_addr >= (DWORD) own_base && ret_addr < (DWORD) own_base + own_size)
    {
        return TRUE;
    }
    else if (ret_addr >= (DWORD) schannel_info.lpBaseOfDll &&
             ret_addr < (DWORD) schannel_info.lpBaseOfDll + schannel_info.SizeOfImage)
    {
        return TRUE;
    }
    else if (ret_addr >= (DWORD) crypt32_info.lpBaseOfDll &&
             ret_addr < (DWORD) crypt32_info.lpBaseOfDll + crypt32_info.SizeOfImage)
	{
		return TRUE;
	}

    return FALSE;
}

static const char *
alg_id_to_string(ALG_ID alg_id)
{
    switch (alg_id)
    {
        case CALG_MD2: return "MD2";
        case CALG_MD4: return "MD4";
        case CALG_MD5: return "MD5";
        case CALG_SHA1: return "SHA1";
        case CALG_MAC: return "MAC";
        case CALG_RSA_SIGN: return "RSA_SIGN";
        case CALG_DSS_SIGN: return "DSS_SIGN";
        case CALG_NO_SIGN: return "NO_SIGN";
        case CALG_RSA_KEYX: return "RSA_KEYX";
        case CALG_DES: return "DES";
        case CALG_3DES_112: return "3DES_112";
        case CALG_3DES: return "3DES";
        case CALG_DESX: return "DESX";
        case CALG_RC2: return "RC2";
        case CALG_RC4: return "RC4";
        case CALG_SEAL: return "SEAL";
        case CALG_DH_SF: return "DH_SF";
        case CALG_DH_EPHEM: return "DH_EPHEM";
        case CALG_AGREEDKEY_ANY: return "AGREEDKEY_ANY";
        case CALG_KEA_KEYX: return "KEA_KEYX";
        case CALG_HUGHES_MD5: return "HUGHES_MD5";
        case CALG_SKIPJACK: return "SKIPJACK";
        case CALG_TEK: return "TEK";
        case CALG_CYLINK_MEK: return "CYLINK_MEK";
        case CALG_SSL3_SHAMD5: return "SSL3_SHAMD5";
        case CALG_SSL3_MASTER: return "SSL3_MASTER";
        case CALG_SCHANNEL_MASTER_HASH: return "SCHANNEL_MASTER_HASH";
        case CALG_SCHANNEL_MAC_KEY: return "SCHANNEL_MAC_KEY";
        case CALG_SCHANNEL_ENC_KEY: return "SCHANNEL_ENC_KEY";
        case CALG_PCT1_MASTER: return "PCT1_MASTER";
        case CALG_SSL2_MASTER: return "SSL2_MASTER";
        case CALG_TLS1_MASTER: return "TLS1_MASTER";
        case CALG_RC5: return "RC5";
        case CALG_HMAC: return "HMAC";
        case CALG_TLS1PRF: return "TLS1PRF";
        case CALG_HASH_REPLACE_OWF: return "HASH_REPLACE_OWF";
        case CALG_AES_128: return "AES_128";
        case CALG_AES_192: return "AES_192";
        case CALG_AES_256: return "AES_256";
        case CALG_AES: return "AES";
        case CALG_SHA_256: return "SHA_256";
        case CALG_SHA_384: return "SHA_384";
        case CALG_SHA_512: return "SHA_512";
        default: break;
    }

    return "UNKNOWN";
}

static const char *
key_param_to_string(DWORD param)
{
    switch (param)
    {
        case KP_ALGID: return "ALGID";
        case KP_BLOCKLEN: return "BLOCKLEN";
        case KP_KEYLEN: return "KEYLEN";
        case KP_SALT: return "SALT";
        case KP_PERMISSIONS: return "PERMISSIONS";
        case KP_P: return "P";
        case KP_Q: return "Q";
        case KP_G: return "G";
        case KP_EFFECTIVE_KEYLEN: return "EFFECTIVE_KEYLEN";
        case KP_IV: return "IV";
        case KP_PADDING: return "PADDING";
        case KP_MODE: return "MODE";
        case KP_MODE_BITS: return "MODE_BITS";
        default: break;
    }

    return "UNKNOWN";
}

static BOOL __cdecl
CryptImportKey_called(BOOL carry_on,
                      DWORD ret_addr,
                      HCRYPTPROV hProv,
                      BYTE *pbData,
                      DWORD dwDataLen,
                      HCRYPTKEY hPubKey,
                      DWORD dwFlags,
                      HCRYPTKEY *phKey)
{
    return TRUE;
}

static BOOL __stdcall
CryptImportKey_done(BOOL retval,
                    HCRYPTPROV hProv,
                    BYTE *pbData,
                    DWORD dwDataLen,
                    HCRYPTKEY hPubKey,
                    DWORD dwFlags,
                    HCRYPTKEY *phKey)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        message_logger_log("CryptImportKey", (char *) &retval - 4, (DWORD) *phKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, (const char *) pbData, dwDataLen,
            "hProv=0x%p, hPubKey=0x%p, dwFlags=0x%08x => *phKey=0x%p",
            hProv, hPubKey, dwFlags, *phKey);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptExportKey_called(BOOL carry_on,
                      DWORD ret_addr,
                      HCRYPTKEY hKey,
                      HCRYPTKEY hExpKey,
                      DWORD dwBlobType,
                      DWORD dwFlags,
                      BYTE *pbData,
                      DWORD *pdwDataLen)
{
    return TRUE;
}

static BOOL __stdcall
CryptExportKey_done(BOOL retval,
                    HCRYPTKEY hKey,
                    HCRYPTKEY hExpKey,
                    DWORD dwBlobType,
                    DWORD dwFlags,
                    BYTE *pbData,
                    DWORD *pdwDataLen)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        const char *blob_type_str;

        switch (dwBlobType)
        {
            case OPAQUEKEYBLOB:
                blob_type_str = "OPAQUEKEYBLOB";
                break;
            case PRIVATEKEYBLOB:
                blob_type_str = "PRIVATEKEYBLOB";
                break;
            case PUBLICKEYBLOB:
                blob_type_str = "PUBLICKEYBLOB";
                break;
            case SIMPLEBLOB:
                blob_type_str = "SIMPLEBLOB";
                break;
            case PLAINTEXTKEYBLOB:
                blob_type_str = "PLAINTEXTKEYBLOB";
                break;
            case SYMMETRICWRAPKEYBLOB:
                blob_type_str = "SYMMETRICWRAPKEYBLOB";
                break;
            default:
                blob_type_str = "UNKNOWN";
                break;
        }

        message_logger_log("CryptExportKey", (char *) &retval - 4, (DWORD) hKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, (const char *) pbData, *pdwDataLen,
            "hKey=0x%p, hExpKey=0x%p, dwBlobType=%s, dwFlags=0x%08x",
            hKey, hExpKey, blob_type_str, dwFlags);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptGenKey_called(BOOL carry_on,
                   DWORD ret_addr,
                   HCRYPTPROV hProv,
                   ALG_ID Algid,
                   DWORD dwFlags,
                   HCRYPTKEY *phKey)
{
    return TRUE;
}

static BOOL __stdcall
CryptGenKey_done(BOOL retval,
                 HCRYPTPROV hProv,
                 ALG_ID Algid,
                 DWORD dwFlags,
                 HCRYPTKEY *phKey)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        message_logger_log("CryptGenKey", (char *) &retval - 4, (DWORD) *phKey,
            MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, NULL, 0,
            "hProv=0x%p, Algid=%s, dwFlags=0x%08x => *phKey=0x%p",
            hProv, alg_id_to_string(Algid), dwFlags, *phKey);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptDuplicateKey_called(BOOL carry_on,
                         DWORD ret_addr,
                         HCRYPTKEY hKey,
                         DWORD *pdwReserved,
                         DWORD dwFlags,
                         HCRYPTKEY *phKey)
{
    return TRUE;
}

static BOOL __stdcall
CryptDuplicateKey_done(BOOL retval,
                       HCRYPTKEY hKey,
                       DWORD *pdwReserved,
                       DWORD dwFlags,
                       HCRYPTKEY *phKey)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        message_logger_log("CryptDuplicateKey", (char *) &retval - 4, (DWORD) *phKey,
            MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, NULL, 0,
            "hKey=0x%p => *phKey=0x%p",
            hKey, *phKey);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptGetKeyParam_called(BOOL carry_on,
                        DWORD ret_addr,
                        HCRYPTKEY hKey,
                        DWORD dwParam,
                        BYTE *pbData,
                        DWORD *pdwDataLen,
                        DWORD dwFlags)
{
    return TRUE;
}

static BOOL __stdcall
CryptGetKeyParam_done(BOOL retval,
                      HCRYPTKEY hKey,
                      DWORD dwParam,
                      BYTE *pbData,
                      DWORD *pdwDataLen,
                      DWORD dwFlags)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        message_logger_log("CryptGetKeyParam", (char *) &retval - 4, (DWORD) hKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, (const char *) pbData, *pdwDataLen,
            "hKey=0x%p, dwParam=%s", hKey, key_param_to_string(dwParam));
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptSetKeyParam_called(BOOL carry_on,
                        DWORD ret_addr,
                        HCRYPTKEY hKey,
                        DWORD dwParam,
                        BYTE *pbData,
                        DWORD dwFlags)
{
    return TRUE;
}

static BOOL __stdcall
CryptSetKeyParam_done(BOOL retval,
                      HCRYPTKEY hKey,
                      DWORD dwParam,
                      BYTE *pbData,
                      DWORD dwFlags)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        const char *data = NULL;
        int data_len = 0;
        DWORD block_len, len = 4;

        switch (dwParam)
        {
            case KP_IV:
                if (CryptGetKeyParam(hKey, KP_BLOCKLEN, (BYTE *) &block_len, &len, 0))
                {
                    data = (const char *) pbData;
                    data_len = block_len / 8;
                }
                break;
            case KP_PERMISSIONS:
            case KP_ALGID:
            case KP_PADDING:
            case KP_MODE:
            case KP_MODE_BITS:
                data = (const char *) pbData;
                data_len = 4;
                break;
            default:
                break;
        }

        message_logger_log("CryptSetKeyParam", (char *) &retval - 4, (DWORD) hKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, data, data_len,
            "hKey=0x%p, dwParam=%s, dwFlags=0x%08x",
            hKey, key_param_to_string(dwParam), dwFlags);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptDestroyKey_called(BOOL carry_on,
                       DWORD ret_addr,
                       HCRYPTKEY hKey)
{
    return TRUE;
}

static BOOL __stdcall
CryptDestroyKey_done(BOOL retval,
                     HCRYPTKEY hKey)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        message_logger_log("CryptDestroyKey", (char *) &retval - 4, (DWORD) hKey,
            MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, NULL, 0, "hKey=0x%p", hKey);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptGenRandom_called(BOOL carry_on,
                      DWORD ret_addr,
                      HCRYPTPROV hProv,
                      DWORD dwLen,
                      BYTE *pbBuffer)
{
    return TRUE;
}

static BOOL __stdcall
CryptGenRandom_done(BOOL retval,
                    HCRYPTPROV hProv,
                    DWORD dwLen,
                    BYTE *pbBuffer)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        message_logger_log("CryptGenRandom", (char *) &retval - 4, (DWORD) hProv,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
            NULL, NULL, (const char *) pbBuffer, dwLen,
            "hProv=0x%p, dwLen=%d", hProv, dwLen);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptEncrypt_called(BOOL carry_on,
                    DWORD ret_addr,
                    HCRYPTKEY hKey,
                    HCRYPTHASH hHash,
                    BOOL Final,
                    DWORD dwFlags,
                    BYTE *pbData,
                    DWORD *pdwDataLen,
                    DWORD dwBufLen)
{
    if (!called_internally(ret_addr))
    {
		void *bt_address = (char *) &carry_on + 8 + CRYPT_ENCRYPT_ARGS_SIZE;

        message_logger_log("CryptEncrypt", bt_address, (DWORD) hKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_OUTGOING,
            NULL, NULL, (const char *) pbData, *pdwDataLen,
            "hKey=0x%p, hHash=0x%p, Final=%s, dwFlags=0x%08x, *pdwDataLen=%d, dwBufLen=%d",
            hKey, hHash, (Final) ? "TRUE" : "FALSE", dwFlags, *pdwDataLen, dwBufLen);
    }

    return TRUE;
}

static BOOL __stdcall
CryptEncrypt_done(BOOL retval,
                  HCRYPTKEY hKey,
                  HCRYPTHASH hHash,
                  BOOL Final,
                  DWORD dwFlags,
                  BYTE *pbData,
                  DWORD *pdwDataLen,
                  DWORD dwBufLen)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (!called_internally(ret_addr))
    {
        message_logger_log("CryptEncrypt", (char *) &retval - 4, (DWORD) hKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INCOMING,
            NULL, NULL, (const char *) pbData, *pdwDataLen,
            "hKey=0x%p, hHash=0x%p, Final=%s, dwFlags=0x%08x, *pdwDataLen=%d, dwBufLen=%d",
            hKey, hHash, (Final) ? "TRUE" : "FALSE", dwFlags, *pdwDataLen, dwBufLen);
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptDecrypt_called(BOOL carry_on,
                    DWORD ret_addr,
                    HCRYPTKEY hKey,
                    HCRYPTHASH hHash,
                    BOOL Final,
                    DWORD dwFlags,
                    BYTE *pbData,
                    DWORD *pdwDataLen)
{
    if (!called_internally(ret_addr))
    {
		void *bt_address = (char *) &carry_on + 8 + CRYPT_DECRYPT_ARGS_SIZE;

        message_logger_log("CryptDecrypt", bt_address, (DWORD) hKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_OUTGOING,
            NULL, NULL, (const char *) pbData, *pdwDataLen,
            "hKey=0x%p, hHash=0x%p, Final=%s, dwFlags=0x%08x, *pdwDataLen=%d",
            hKey, hHash, (Final) ? "TRUE" : "FALSE", dwFlags, *pdwDataLen);
    }

    return TRUE;
}

static BOOL __stdcall
CryptDecrypt_done(BOOL retval,
                  HCRYPTKEY hKey,
                  HCRYPTHASH hHash,
                  BOOL Final,
                  DWORD dwFlags,
                  BYTE *pbData,
                  DWORD *pdwDataLen)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (!called_internally(ret_addr))
    {
        message_logger_log("CryptDecrypt", (char *) &retval - 4, (DWORD) hKey,
            MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INCOMING,
            NULL, NULL, (const char *) pbData, *pdwDataLen,
            "hKey=0x%p, hHash=0x%p, Final=%s, dwFlags=0x%08x, *pdwDataLen=%d",
            hKey, hHash, (Final) ? "TRUE" : "FALSE", dwFlags, *pdwDataLen);
    }

    SetLastError(err);
    return retval;
}

class HashContext : public BaseObject
{
protected:
    DWORD id;
    ALG_ID alg_id;

public:
    HashContext(ALG_ID alg_id)
    {
        this->id = ospy_rand();
        this->alg_id = alg_id;
    }

    DWORD get_id()
    {
        return id;
    }

    ALG_ID get_alg_id()
    {
        return alg_id;
    }

    const char *get_alg_id_as_string()
    {
        return alg_id_to_string(alg_id);
    }
};

typedef map<HCRYPTHASH, HashContext *, less<HCRYPTHASH>, MyAlloc<pair<HCRYPTHASH, HashContext *>>> HashMap;

static CRITICAL_SECTION cs;
static HashMap hash_map;

#define LOCK() EnterCriticalSection(&cs)
#define UNLOCK() LeaveCriticalSection(&cs)

static BOOL __cdecl
CryptCreateHash_called(BOOL carry_on,
                       DWORD ret_addr,
                       HCRYPTPROV hProv,
                       ALG_ID Algid,
                       HCRYPTKEY hKey,
                       DWORD dwFlags,
                       HCRYPTHASH *phHash)
{
    return TRUE;
}

static BOOL __stdcall
CryptCreateHash_done(BOOL retval,
                     HCRYPTPROV hProv,
                     ALG_ID Algid,
                     HCRYPTKEY hKey,
                     DWORD dwFlags,
                     HCRYPTHASH *phHash)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval)
    {
        HashContext *ctx = new HashContext(Algid);

        LOCK();

        hash_map[*phHash] = ctx;

        if (!called_internally(ret_addr))
        {
            message_logger_log("CryptCreateHash", (char *) &retval - 4, ctx->get_id(),
                MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
                NULL, NULL, NULL, 0,
                "hProv=0x%p, Algid=%s, hKey=0x%p => *phHash=0x%p",
                hProv, ctx->get_alg_id_as_string(), hKey, *phHash);
        }

        UNLOCK();
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptDuplicateHash_called(BOOL carry_on,
                          DWORD ret_addr,
                          HCRYPTHASH hHash,
                          DWORD *pdwReserved,
                          DWORD dwFlags,
                          HCRYPTHASH *phHash)
{
    return TRUE;
}

static BOOL __stdcall
CryptDuplicateHash_done(BOOL retval,
                        HCRYPTHASH hHash,
                        DWORD *pdwReserved,
                        DWORD dwFlags,
                        HCRYPTHASH *phHash)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        HashMap::iterator iter;

        LOCK();

        iter = hash_map.find(hHash);
        if (iter != hash_map.end())
        {
            HashContext *ctx = iter->second;

            message_logger_log("CryptDuplicateHash", (char *) &retval - 4, ctx->get_id(),
                MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
                NULL, NULL, NULL, 0,
                "hHash=0x%p, Algid=%s => *phHash=0x%p",
                hHash, ctx->get_alg_id_as_string(), *phHash);
        }

        UNLOCK();
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptDestroyHash_called(BOOL carry_on,
                        DWORD ret_addr,
                        HCRYPTHASH hHash)
{
    return TRUE;
}

static BOOL __stdcall
CryptDestroyHash_done(BOOL retval,
                      HCRYPTHASH hHash)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval)
    {
        LOCK();

        HashMap::iterator iter = hash_map.find(hHash);
        if (iter != hash_map.end())
        {
            HashContext *ctx = iter->second;

            if (!called_internally(ret_addr))
            {
                message_logger_log("CryptDestroyHash", (char *) &retval - 4, ctx->get_id(),
                    MESSAGE_TYPE_MESSAGE, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
                    NULL, NULL, NULL, 0, "hHash=0x%p", hHash);
            }

            hash_map.erase(iter);
            delete ctx;
        }

        UNLOCK();
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptHashData_called(BOOL carry_on,
                     DWORD ret_addr,
                     HCRYPTHASH hHash,
                     BYTE *pbData,
                     DWORD dwDataLen,
                     DWORD dwFlags)
{
    return TRUE;
}

static BOOL __stdcall
CryptHashData_done(BOOL retval,
                   HCRYPTHASH hHash,
                   BYTE *pbData,
                   DWORD dwDataLen,
                   DWORD dwFlags)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        HashMap::iterator iter;

        LOCK();

        iter = hash_map.find(hHash);
        if (iter != hash_map.end())
        {
            HashContext *ctx = iter->second;

            message_logger_log("CryptHashData", (char *) &retval - 4, ctx->get_id(),
                MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
                NULL, NULL, (const char *) pbData, dwDataLen,
                "hHash=0x%p, Algid=%s", hHash, ctx->get_alg_id_as_string());
        }

        UNLOCK();
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptGetHashParam_called (BOOL carry_on,
                          DWORD ret_addr,
                          HCRYPTHASH hHash,
                          DWORD dwParam,
                          BYTE *pbData,
                          DWORD *pdwDataLen,
                          DWORD dwFlags)
{
    return TRUE;
}

static BOOL __stdcall
CryptGetHashParam_done (BOOL retval,
                        HCRYPTHASH hHash,
                        DWORD dwParam,
                        BYTE *pbData,
                        DWORD *pdwDataLen,
                        DWORD dwFlags)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        HashMap::iterator iter;

        LOCK();

        iter = hash_map.find(hHash);
        if (iter != hash_map.end())
        {
            HashContext *ctx = iter->second;
            const char *param_str;

            switch (dwParam)
            {
                case HP_ALGID:
                    param_str = "ALGID";
                    break;
                case HP_HASHSIZE:
                    param_str = "HASHSIZE";
                    break;
                case HP_HASHVAL:
                    param_str = "HASHVAL";
                    break;
                default:
                    param_str = "UNKNOWN";
                    break;
            }

            message_logger_log("CryptGetHashParam", (char *) &retval - 4, ctx->get_id(),
                MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
                NULL, NULL, (const char *) pbData, *pdwDataLen,
                "hHash=0x%p, Algid=%s, dwParam=%s",
                hHash, ctx->get_alg_id_as_string(), param_str);
        }

        UNLOCK();
    }

    SetLastError(err);
    return retval;
}

static BOOL __cdecl
CryptSetHashParam_called(BOOL carry_on,
                         DWORD ret_addr,
                         HCRYPTHASH hHash,
                         DWORD dwParam,
                         BYTE *pbData,
                         DWORD dwFlags)
{
    return TRUE;
}

static BOOL __stdcall
CryptSetHashParam_done(BOOL retval,
                       HCRYPTHASH hHash,
                       DWORD dwParam,
                       BYTE *pbData,
                       DWORD dwFlags)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    if (retval && !called_internally(ret_addr))
    {
        HashMap::iterator iter;

        LOCK();

        iter = hash_map.find(hHash);
        if (iter != hash_map.end())
        {
            HashContext *ctx = iter->second;
            const char *param_str;
            HMAC_INFO *hmi;
            ByteBuffer *buf = NULL;
            const char *data = NULL;
            int data_len = 0;

            switch (dwParam)
            {
                case HP_HMAC_INFO:
                    param_str = "HMAC_INFO";
                    hmi = (HMAC_INFO *) pbData;

                    buf = byte_buffer_sized_new(4 +
                                                4 + hmi->cbInnerString +
                                                4 + hmi->cbOuterString);

                    byte_buffer_append(buf, &hmi->HashAlgid, sizeof(ALG_ID));

                    byte_buffer_append(buf, &hmi->cbInnerString, sizeof(DWORD));
                    byte_buffer_append(buf, hmi->pbInnerString, hmi->cbInnerString);

                    byte_buffer_append(buf, &hmi->cbOuterString, sizeof(DWORD));
                    byte_buffer_append(buf, hmi->pbOuterString, hmi->cbOuterString);

                    data = (const char *) buf->buf;
                    data_len = (int) buf->offset;

                    break;
                case HP_HASHVAL:
                    param_str = "HASHVAL";
                    break;
                default:
                    param_str = "UNKNOWN";
                    break;
            }

            message_logger_log("CryptSetHashParam", (char *) &retval - 4, ctx->get_id(),
                MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO, PACKET_DIRECTION_INVALID,
                NULL, NULL, data, data_len,
                "hHash=0x%p, Algid=%s, dwParam=%s",
                hHash, ctx->get_alg_id_as_string(), param_str);

            if (buf != NULL)
                byte_buffer_free(buf);
        }

        UNLOCK();
    }

    SetLastError(err);
    return retval;
}

HOOK_GLUE_SPECIAL(CryptImportKey, (6 * 4))
HOOK_GLUE_SPECIAL(CryptExportKey, (6 * 4))
HOOK_GLUE_SPECIAL(CryptGenKey, (4 * 4))
HOOK_GLUE_SPECIAL(CryptDuplicateKey, (4 * 4))
HOOK_GLUE_SPECIAL(CryptGetKeyParam, (5 * 4))
HOOK_GLUE_SPECIAL(CryptSetKeyParam, (4 * 4))
HOOK_GLUE_SPECIAL(CryptDestroyKey, (1 * 4))

HOOK_GLUE_SPECIAL(CryptGenRandom, (3 * 4))

HOOK_GLUE_SPECIAL(CryptEncrypt, CRYPT_ENCRYPT_ARGS_SIZE)
HOOK_GLUE_SPECIAL(CryptDecrypt, CRYPT_DECRYPT_ARGS_SIZE)

HOOK_GLUE_SPECIAL(CryptCreateHash, (5 * 4))
HOOK_GLUE_SPECIAL(CryptDuplicateHash, (4 * 4))
HOOK_GLUE_SPECIAL(CryptDestroyHash, (1 * 4))
HOOK_GLUE_SPECIAL(CryptHashData, (4 * 4))
HOOK_GLUE_SPECIAL(CryptGetHashParam, (5 * 4))
HOOK_GLUE_SPECIAL(CryptSetHashParam, (4 * 4))

void
hook_crypt()
{
    // Initialize
    InitializeCriticalSection(&cs);

    if (!get_module_base_and_size("oSpyAgent.dll", &own_base, &own_size, NULL))
    {
        message_logger_log_message("hook_crypt", 0, MESSAGE_CTX_WARNING,
                                   "get_module_base_and_size for self failed");
    }

    HMODULE h = LoadLibrary("schannel.dll");
    if (h == NULL)
    {
        MessageBox(0, "Failed to load 'schannel.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    if (GetModuleInformation(GetCurrentProcess(), h, &schannel_info,
                             sizeof(schannel_info)) == 0)
    {
        message_logger_log_message("hook_crypt", 0, MESSAGE_CTX_WARNING,
                                   "GetModuleInformation failed with errno %d",
                                   GetLastError());
    }

	h = LoadLibrary("crypt32.dll");
    if (h == NULL)
    {
        MessageBox(0, "Failed to load 'crypt32.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    if (GetModuleInformation(GetCurrentProcess(), h, &crypt32_info,
                             sizeof(crypt32_info)) == 0)
    {
        message_logger_log_message("hook_crypt", 0, MESSAGE_CTX_WARNING,
                                   "GetModuleInformation failed with errno %d",
                                   GetLastError());
    }

    // Hook the Crypt API
    h = LoadLibrary("advapi32.dll");
    if (h == NULL)
    {
	    MessageBox(0, "Failed to load 'advapi32.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
        return;
    }

    HOOK_FUNCTION_SPECIAL(h, CryptImportKey);
    HOOK_FUNCTION_SPECIAL(h, CryptExportKey);
    HOOK_FUNCTION_SPECIAL(h, CryptGenKey);
    HOOK_FUNCTION_SPECIAL(h, CryptDuplicateKey);
    HOOK_FUNCTION_SPECIAL(h, CryptGetKeyParam);
    HOOK_FUNCTION_SPECIAL(h, CryptSetKeyParam);
    HOOK_FUNCTION_SPECIAL(h, CryptDestroyKey);

    HOOK_FUNCTION_SPECIAL(h, CryptGenRandom);

    HOOK_FUNCTION_SPECIAL(h, CryptEncrypt);
    HOOK_FUNCTION_SPECIAL(h, CryptDecrypt);

    HOOK_FUNCTION_SPECIAL(h, CryptCreateHash);
    HOOK_FUNCTION_SPECIAL(h, CryptDuplicateHash);
    HOOK_FUNCTION_SPECIAL(h, CryptDestroyHash);
    HOOK_FUNCTION_SPECIAL(h, CryptHashData);
    HOOK_FUNCTION_SPECIAL(h, CryptGetHashParam);
    HOOK_FUNCTION_SPECIAL(h, CryptSetHashParam);
}

#pragma managed(pop)
