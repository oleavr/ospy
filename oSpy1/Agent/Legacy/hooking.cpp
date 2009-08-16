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
#include "util.h"
#include "logging.h"

#pragma managed(push, off)

bool
CHookContext::ShouldLog(void *returnAddress, CpuContext *ctx, ...)
{
    if (m_retAddrs.find(returnAddress) == m_retAddrs.end())
        return true;

    HookRetAddrShouldLogFunc func = m_retAddrs[returnAddress];
    if (func == NULL)
        return false;

    va_list args;
    va_start(args, ctx);
    return func(ctx, args);
}

#define MAKEPTR(p,o) (LPVOID) ( (DWORD)p + (DWORD)o )
#define WRITE_OPCODE(pCode, x) \
   res = WriteProcessMemory( hProcess, pCode, &x, sizeof(x), &nWritten ); \
   if( !res ) return false; \
   pCode = MAKEPTR(pCode,sizeof(x))
#define WRITE_DWORD(pCode, x) \
   res = WriteProcessMemory( hProcess, pCode, &x, sizeof(x), &nWritten ); \
   if( !res ) return false; \
   pCode = MAKEPTR(pCode,sizeof(x))

void
write_byte_to_addr(LPVOID lpAddr, BYTE b)
{
  HANDLE hProcess;
    DWORD dwOldProtect, nWritten;
    
  hProcess = GetCurrentProcess();

  VirtualProtect(lpAddr, 1, PAGE_EXECUTE_WRITECOPY, &dwOldProtect);

  WriteProcessMemory(hProcess, lpAddr, &b, 1, &nWritten);

  FlushInstructionCache(hProcess, NULL, 0);
}

void
write_dword_to_addr(LPVOID lpAddr, DWORD dw)
{
  HANDLE hProcess;
    DWORD dwOldProtect, nWritten;
    
  hProcess = GetCurrentProcess();

  VirtualProtect(lpAddr, 4, PAGE_EXECUTE_WRITECOPY, &dwOldProtect);

  WriteProcessMemory(hProcess, lpAddr, &dw, 4, &nWritten);

  FlushInstructionCache(hProcess, NULL, 0);
}

bool
write_jmp_instruction_to_addr(LPVOID lpOrgProc, LPVOID lpNewProc)
{
   HANDLE hProcess;
   DWORD nWritten;
   DWORD dwOldProtect;
   LPVOID pCode;
   BOOL res;
   DWORD dwAdr;
   BYTE opcode_jmp = 0xE9;

   hProcess = GetCurrentProcess();

   VirtualProtect(lpOrgProc, 5, PAGE_EXECUTE_WRITECOPY, &dwOldProtect);

   pCode = MAKEPTR(lpOrgProc, 0);
   WRITE_OPCODE(pCode, opcode_jmp);
   dwAdr = (DWORD)lpNewProc - (DWORD)pCode - sizeof(dwAdr);
   WRITE_DWORD(pCode,dwAdr);

   FlushInstructionCache(hProcess,NULL,0);

   return true;
};

typedef enum {
    TOKEN_TYPE_LITERAL = 0,
    TOKEN_TYPE_IGNORE = 1,
} SignatureTokenType;

typedef struct {
    SignatureTokenType type;
    int length;
    unsigned char *data;
} SignatureToken;

static bool
find_next_token(const char **input, int *num_ignores, char **error)
{
    const char *p = *input;
    int chars_skipped = 0, qms_skipped = 0;
    *num_ignores = 0;

    for (; *p != '\0'; p++)
    {
        if ((*p >= 48 && *p <= 57) ||
            (*p >= 65 && *p <= 90) ||
            (*p >= 97 && *p <= 122))
        {
            chars_skipped++;

            if (chars_skipped == 3)
            {
                goto DONE;
            }
        }
        else if (*p == '?')
        {
            qms_skipped++;
        }
    }

    if (*p == '\0')
        p = NULL;

DONE:
    *input = p;
    *num_ignores = qms_skipped / 2;
    return true;
}

static SignatureToken *
signature_token_new (SignatureTokenType type, int buffer_size)
{
    SignatureToken *token;

    token = (SignatureToken *) sspy_malloc(sizeof(SignatureToken));
    token->type = type;
    token->length = 0;

    if (buffer_size > 0)
        token->data = (unsigned char *) sspy_malloc(buffer_size);
    else
        token->data = NULL;

    return token;
}

static void
signature_token_free (SignatureToken *token)
{
    if (token->data != NULL)
    {
        sspy_free(token->data);
    }

    sspy_free(token);
}

static void
signature_token_list_free (SignatureToken **tokens)
{
    int i;

    for (i = 0; i; i++)
    {
        SignatureToken *cur_token = tokens[i];

        if (cur_token != NULL)
        {
            signature_token_free(cur_token);
        }
        else
        {
            break;
        }
    }

    sspy_free(tokens);
}

static void
signature_token_print (SignatureToken *token)
{
    fprintf(stderr, "type=%s, length=%d",
        (token->type == TOKEN_TYPE_LITERAL) ? "TOKEN_TYPE_LITERAL" : "TOKEN_TYPE_IGNORE ",
        token->length);

    if (token->data != NULL)
    {
        int j;

        fprintf(stderr, ", data=[");
        for (j = 0; j < token->length; j++)
        {
            fprintf(stderr, " %02hhx", token->data[j]);
        }

        fprintf(stderr, "]");
    }

    fprintf(stderr, "\n");
}

static bool
parse_signature(const FunctionSignature *sig,
                SignatureToken ***tokens,
                int *num_tokens,
                int *raw_size,
                SignatureToken **longest_token,
                int *longest_offset,
                char **error)
{
    const char *p;
    int sig_len, max_tokens, token_index, buf_pos, i, offset;
    SignatureToken **ret_tokens, *cur_token;

    ret_tokens = NULL;

    /* Count the number of '?' characters in order to get
     * an idea of the maximum number of tokens we're
     * going to parse, and find the string length while
     * we're at it. */
    sig_len = max_tokens = 0;
    for (p = sig->signature; *p != '\0'; p++)
    {
        sig_len++;

        if (*p == '?')
            max_tokens++;
    }

    if (max_tokens == 0)
    {
        max_tokens++;
    }
    else
    {
        if (max_tokens % 2 != 0)
        {
            *error = sspy_strdup("syntax error: unbalanced questionmarks");
            goto ERR_OUT;
        }

        max_tokens = (max_tokens / 2) + 1;
    }

    /* Make it NULL-terminated. */
    max_tokens++;

    /* Allocate an array of token-pointers. */
    ret_tokens = (SignatureToken **) sspy_malloc(max_tokens * sizeof(SignatureToken *));
    memset(ret_tokens, 0, max_tokens * sizeof(SignatureToken *));
    cur_token = NULL;
    token_index = 0;

    /* Start parsing. */
    p = sig->signature;
    while (true)
    {
        unsigned int tmp;
        int ret, ignores;

        ret = sscanf(p, " %02x", &tmp);
        if (ret != 1)
        {
            break;
        }

        if (cur_token == NULL)
        {
            cur_token = signature_token_new(TOKEN_TYPE_LITERAL, sig_len + 1);
            buf_pos = 0;
        }

        cur_token->data[buf_pos] = tmp;
        cur_token->length++;
        buf_pos++;

        if (!find_next_token(&p, &ignores, error))
        {
            goto ERR_OUT;
        }

        if (ignores > 0)
        {
            ret_tokens[token_index++] = cur_token;

            cur_token = signature_token_new(TOKEN_TYPE_IGNORE, 0);
            cur_token->length = ignores;
            ret_tokens[token_index++] = cur_token;

            cur_token = NULL;
        }

        if (p == NULL)
        {
            break;
        }
    }

    if (cur_token != NULL)
    {
        ret_tokens[token_index++] = cur_token;
    }

    *tokens = ret_tokens;
    *num_tokens = token_index;

    /* Find the longest token and its offset. */
    offset = 0;
    *longest_token = ret_tokens[0];
    *longest_offset = 0;
    for (i = 0; i < *num_tokens; i++)
    {
        cur_token = ret_tokens[i];

        if (cur_token->type == TOKEN_TYPE_LITERAL)
        {
            if (cur_token->length > (*longest_token)->length)
            {
                *longest_token = cur_token;
                *longest_offset = offset;
            }
        }

        offset += cur_token->length;
    }

    *raw_size = offset;
    
    return true;

ERR_OUT:
    if (ret_tokens != NULL)
    {
        signature_token_list_free(ret_tokens);
    }

    return false;
}

static bool
scan_against_all_tokens(unsigned char *p, SignatureToken **tokens)
{
    SignatureToken **cur_token;

    for (cur_token = tokens; *cur_token != NULL; cur_token++)
    {
        if ((*cur_token)->type == TOKEN_TYPE_LITERAL)
        {
            if (memcmp(p, (*cur_token)->data, (*cur_token)->length) != 0)
                return false;
        }

        p += (*cur_token)->length;
    }

    return true;
}

bool
find_signature(const FunctionSignature *sig, LPVOID *address, char **error)
{
    return find_signature_in_module(sig, sig->module_name, address, error);
}

bool
find_signature_in_module(const FunctionSignature *sig, const char *module_name, LPVOID *address, char **error)
{
    bool result = false;
    unsigned char *base, *p, *match;
    DWORD size;
    SignatureToken **tokens = NULL, *longest_token;
    int num_tokens, raw_size, longest_offset, num_matches;

    if (!get_module_base_and_size(module_name, (LPVOID *) &base, &size, error))
    {
        goto DONE;
    }

    if (!parse_signature(sig, &tokens, &num_tokens, &raw_size, &longest_token,
                         &longest_offset, error))
    {
        goto DONE;
    }

    match = NULL;
    num_matches = 0;
    for (p = base; p < base + size - raw_size; p++)
    {
        if (memcmp(p, longest_token->data, longest_token->length) == 0)
        {
            bool matched = scan_against_all_tokens(p - longest_offset, tokens);
            if (matched)
            {
                num_matches++;
                match = p - longest_offset;

                /* Skip ahead. */
                p = p - longest_offset + raw_size;
            }
        }
    }

    if (num_matches == 0)
    {
        *error = sspy_strdup("No matches found");
        goto DONE;
    }
    else if (num_matches > 1)
    {
        *error = sspy_strdup("More than one match found");
        goto DONE;
    }

    *address = match - sig->start_offset;

    result = true;

DONE:
    if (tokens != NULL)
    {
        signature_token_list_free(tokens);
    }

    return result;
}

bool
override_function_by_signature(const FunctionSignature *sig,
                               LPVOID replacement,
                               LPVOID *patched_address,
                               char **error)
{
    return override_function_by_signature_in_module(sig, sig->module_name,
        replacement, patched_address, error);
}

bool
override_function_by_signature_in_module(const FunctionSignature *sig,
                                         const char *module_name,
                                         LPVOID replacement,
                                         LPVOID *patched_address,
                                         char **error)
{
    LPVOID address;

    if (!find_signature_in_module(sig, module_name, &address, error))
        return false;

    if (!write_jmp_instruction_to_addr(address, replacement))
    {
        *error = sspy_strdup("write_jmp_instruction_to_addr failed");
        return false;
    }

    if (patched_address != NULL)
        *patched_address = address;

    return true;
}

#pragma managed(pop)
