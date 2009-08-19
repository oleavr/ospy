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

#include <udis86.h>

#define OPCODE_NOP (0x90)

#pragma managed(push, off)

static HookManager *g_hookMgr = NULL;

void
HookManager::Init()
{
    g_hookMgr = new HookManager();
}

void
HookManager::Uninit()
{
    delete g_hookMgr;
    g_hookMgr = NULL;
}

HookManager *
HookManager::Obtain()
{
    return g_hookMgr;
}

void
HookManager::Add(void *address, DWORD size)
{
    CodeFragment frag(address, size);
    m_fragments.push_back(frag);
}

void
HookManager::RevertAll()
{
    OVector<CodeFragment>::Type::iterator it;
    for (it = m_fragments.begin(); it != m_fragments.end(); ++it)
    {
        it->Revert();
    }
}

CodeFragment::CodeFragment(void *address, DWORD size)
    : m_address(address)
{
    m_originalCode.resize(size);

    DWORD oldProt;
    VirtualProtect(address, size, PAGE_EXECUTE_READ, &oldProt);
    memcpy(const_cast<char *>(m_originalCode.data()), address, size);
    VirtualProtect(address, size, oldProt, &oldProt);
}

void
CodeFragment::Revert()
{
    DWORD oldProt;
    VirtualProtect(m_address, m_originalCode.size(), PAGE_EXECUTE_WRITECOPY, &oldProt);
    memcpy(m_address, m_originalCode.data(), m_originalCode.size());
    VirtualProtect(m_address, m_originalCode.size(), oldProt, &oldProt);
}

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

class SignatureToken : public BaseObject
{
public:
    SignatureToken(SignatureTokenType type)
        : type(type), length(0)
    {
    }

    SignatureTokenType type;
    DWORD length;
    OString data;
};

static bool
parse_signature(const FunctionSignature *sig,
                OVector<SignatureToken>::Type &tokens,
                DWORD *raw_size,
                DWORD *longest_token_idx,
                DWORD *longest_token_offset,
                char **error)
{
    int cur_token_idx = -1;

    const char *p;
    for (p = sig->signature; *p != '\0';)
    {
        if (p[0] == '?')
        {
            if (p[1] != '?')
            {
                *error = sspy_strdup("syntax error: unbalanced questionmarks");
                return false;
            }

            if (cur_token_idx < 0 || tokens[cur_token_idx].type != TOKEN_TYPE_IGNORE)
            {
                SignatureToken tok(TOKEN_TYPE_IGNORE);
                tokens.push_back(tok);
                cur_token_idx = tokens.size() - 1;
            }

            tokens[cur_token_idx].length++;

            p += 2;
        }
        else if (isxdigit(p[0]))
        {
            if (!isxdigit(p[1]))
            {
                *error = sspy_strdup("syntax error: incomplete hex byte");
                return false;
            }

            if (cur_token_idx < 0 || tokens[cur_token_idx].type != TOKEN_TYPE_LITERAL)
            {
                SignatureToken tok(TOKEN_TYPE_LITERAL);
                tokens.push_back(tok);
                cur_token_idx = tokens.size() - 1;
            }

            tokens[cur_token_idx].length++;

            char str[3] = { tolower(p[0]), tolower(p[1]), '\0' };
            int parsed_value;
            sscanf(str, "%02x", &parsed_value);
            BYTE value = parsed_value & 0xff;
            tokens[cur_token_idx].data.append(reinterpret_cast<char *>(&value), sizeof(value));

            p += 2;
        }
        else if (isspace(*p))
        {
            p++;
        }
        else
        {
            *error = sspy_strdup("syntax error: expected byte specifier");
            return false;
        }
    }

    DWORD index = 0, offset = 0;
    DWORD longest = 0;

    OVector<SignatureToken>::Type::const_iterator it;
    for (it = tokens.begin(); it != tokens.end(); ++it)
    {
        if (it->type == TOKEN_TYPE_LITERAL)
        {
            if (it->length > longest)
            {
                *longest_token_idx = index;
                *longest_token_offset = offset;

                longest = it->length;
            }
        }

        index++;
        offset += it->length;
    }

    if (longest == 0)
    {
        *error = sspy_strdup("error: no match bytes specified");
        return false;
    }

    *raw_size = offset;

    return true;
}

static bool
scan_against_all_tokens(unsigned char *p, const OVector<SignatureToken>::Type tokens)
{
    OVector<SignatureToken>::Type::const_iterator it;
    for (it = tokens.begin(); it != tokens.end(); ++it)
    {
        if (it->type == TOKEN_TYPE_LITERAL)
        {
            if (memcmp(p, it->data.data(), it->length) != 0)
                return false;
        }

        p += it->length;
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
    BYTE *base;
    DWORD size;
    if (!get_module_base_and_size(module_name, (LPVOID *) &base, &size, error))
        return false;

    OVector<SignatureToken>::Type tokens;
    DWORD raw_size;
    DWORD longest_token_idx, longest_token_offset;
    if (!parse_signature(sig, tokens, &raw_size, &longest_token_idx, &longest_token_offset, error))
        return false;
    SignatureToken *longest_token = &tokens[longest_token_idx];

    BYTE *p, *match = NULL;
    DWORD num_matches = 0;

    OVector<MEMORY_BASIC_INFORMATION>::Type regions;
    DWORD page_bytes_left = 0;

    for (p = base; p < base + size - raw_size;)
    {
        if (page_bytes_left < raw_size)
        {
            MEMORY_BASIC_INFORMATION mbi;
            SIZE_T result = VirtualQuery(p + page_bytes_left, &mbi, sizeof(mbi));
            _ASSERT(result == sizeof(mbi));

            if ((mbi.Protect & (PAGE_NOACCESS | PAGE_READONLY | PAGE_READWRITE | PAGE_WRITECOPY)) != 0 ||
                (mbi.Protect & PAGE_GUARD) != 0)
            {
                page_bytes_left = 0;
                p += mbi.RegionSize;
                continue;
            }

            if ((mbi.Protect & PAGE_EXECUTE) != 0)
            {
                DWORD old_protect;
                VirtualProtect(p + page_bytes_left, mbi.RegionSize, PAGE_EXECUTE_READWRITE, &old_protect);

                regions.push_back(mbi);
            }

            page_bytes_left += mbi.RegionSize;
        }

        int distance_forward = 1;

        if (memcmp(p, longest_token->data.data(), longest_token->length) == 0)
        {
            bool matched = scan_against_all_tokens(p - longest_token_offset, tokens);
            if (matched)
            {
                num_matches++;
                match = p - longest_token_offset;

                distance_forward = raw_size - longest_token_offset;
            }
        }

        page_bytes_left -= distance_forward;
        p += distance_forward;
    }

    OVector<MEMORY_BASIC_INFORMATION>::Type::const_iterator it;
    for (it = regions.begin(); it != regions.end(); ++it)
    {
        DWORD old_protect;
        VirtualProtect(it->BaseAddress, it->RegionSize, it->Protect, &old_protect);
    }

    if (num_matches == 0)
    {
        *error = sspy_strdup("No matches found");
        return false;
    }
    else if (num_matches > 1)
    {
        *error = sspy_strdup("More than one match found");
        return false;
    }

    *address = match - sig->start_offset;

    return true;
}

static DWORD
round_to_next_instruction(LPVOID address, DWORD code_size)
{
    ud_t obj;
    ud_init(&obj);
    ud_set_input_buffer(&obj, static_cast<uint8_t *>(address), 4096);
    ud_set_mode(&obj, 32);

    DWORD result = 0;
    while (result < code_size)
    {
        unsigned int size = ud_disassemble(&obj);
        _ASSERT(size != 0);

        result += size;
    }

    return result;
}

bool
intercept_code_matching(const FunctionSignature *sig,
                        LPVOID replacement,
                        LPVOID *resume_trampoline,
                        char **error)
{
    LPVOID address;
    if (!find_signature_in_module(sig, sig->module_name, &address, error))
        return false;

    DWORD old_prot;
    VirtualProtect(address, 16, PAGE_EXECUTE_READWRITE, &old_prot);

    const DWORD bytes_needed_for_jump = 5;
    DWORD n_bytes_to_copy = round_to_next_instruction(address, bytes_needed_for_jump);

    *resume_trampoline = VirtualAlloc(NULL, n_bytes_to_copy + bytes_needed_for_jump,
        MEM_COMMIT | MEM_RESERVE,
        PAGE_EXECUTE_READWRITE);
    memcpy(*resume_trampoline, address, n_bytes_to_copy);
    write_jmp_instruction_to_addr(static_cast<char *>(*resume_trampoline) + n_bytes_to_copy,
        static_cast<char *>(address) + n_bytes_to_copy);

    write_jmp_instruction_to_addr(address, replacement);

    unsigned char *pad = static_cast<unsigned char *>(address) + bytes_needed_for_jump;
    for (DWORD remaining = n_bytes_to_copy - bytes_needed_for_jump; remaining != 0; remaining--)
    {
        *pad = OPCODE_NOP;
        pad++;
    }

    VirtualProtect(address, n_bytes_to_copy, old_prot, &old_prot);

    return true;
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
