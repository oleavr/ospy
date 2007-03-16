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

#pragma once

/*
 * WARNING: This code in particular isn't 64-bit compliant, but might
 *          be in the future if I get around to installing a 64-bit
 *          Windows OS (which is highly unlikely considering that I
 *          only use it for REing).  Contributions are very welcome. :-)
 */

#include "util.h"
#include "TrampoLib\TrampoLib.h"

using namespace TrampoLib;

typedef bool (*HookRetAddrShouldLogFunc) (CpuContext *context, va_list args);

class CHookContext
{
public:
	CHookContext() {}

	void RegisterReturnAddress(void *address, HookRetAddrShouldLogFunc func=NULL) { m_retAddrs[address] = func; }

	bool ShouldLog(void *returnAddress, CpuContext *ctx, ...);

protected:
	OMap<void *, HookRetAddrShouldLogFunc>::Type m_retAddrs;
};

typedef struct {
    char *module_name;
	int start_offset;
    char *signature;
} FunctionSignature;


/****************************************************************************
 * Useful hooking macros taking care of all the dirty work                  *
 ****************************************************************************/

#define HOOK_GLUE(name, args_size) \
  static LPVOID name##_start = NULL; \
  \
  __declspec(naked) static void name##_done_proxy() \
  { \
    __asm { \
      /* Store return value (eax) between return address and first argument */ \
      __asm sub esp, 4 \
      __asm push ebp \
      __asm mov ebp, [esp+8] \
      __asm mov [esp+4], ebp \
      __asm mov [esp+8], eax \
      __asm pop ebp \
      \
      /* Call our C callback */ \
      __asm jmp name##_done \
    } \
  } \
  \
  __declspec(naked) static void name##_hook() \
  { \
    __asm { \
      /* Calculate size of retaddr + args */ \
      __asm mov edx, 4            /* edx contains the total size of */ \
      __asm add edx, args_size    /* return address + all arguments. */ \
      \
      __asm mov ecx, edx          /* ecx contains edx divided by 4 */ \
      __asm shr ecx, 2            /* to speed up the copying. */ \
      \
      /* Make a copy of retaddr + args onto the top of the stack */ \
      __asm sub esp, edx \
      __asm push esi \
      __asm push edi \
      __asm cld \
      __asm lea edi, [8 + esp] \
      __asm mov esi, edi \
      __asm add esi, edx \
      __asm rep movsd \
      __asm pop edi \
      __asm pop esi \
      \
      /* Call our C callback */ \
      __asm call name##_called \
      \
      /* Overwrite return address so that we can trap the return. */ \
      __asm mov ecx, name##_done_proxy \
      __asm mov dword ptr [esp], ecx \
      \
      /* Set up stack frame (we need to do this because we've */ \
      /* overwritten this code in the start of the original function) */ \
      __asm push ebp \
      __asm mov ebp, esp \
      \
      /* Continue */ \
      __asm mov ecx, [name##_start] \
      __asm add ecx, 5 \
      __asm jmp ecx \
    } \
  }

#define HOOK_GLUE_INTERRUPTIBLE(name, args_size) \
  static LPVOID name##_start = NULL; \
  \
  __declspec(naked) static void name##_done_proxy() \
  { \
    __asm { \
      /* Store return value (eax) between return address and first argument */ \
      __asm sub esp, 4 \
      __asm push ebp \
      __asm mov ebp, [esp+8] \
      __asm mov [esp+4], ebp \
      __asm mov [esp+8], eax \
      __asm pop ebp \
      \
      /* Call our C callback */ \
      __asm jmp name##_done \
    } \
  } \
  \
  __declspec(naked) static void name##_hook() \
  { \
    __asm { \
      /* Calculate size of retaddr + args */ \
      __asm mov edx, 4            /* edx contains the total size of */ \
      __asm add edx, args_size    /* return address + all arguments. */ \
      \
      __asm mov ecx, edx          /* ecx contains edx divided by 4 */ \
      __asm shr ecx, 2            /* to speed up the copying. */ \
      \
      /* Make a copy of retaddr + args onto the top of the stack */ \
      __asm sub esp, edx \
      __asm push esi \
      __asm push edi \
      __asm cld \
      __asm lea edi, [8 + esp] \
      __asm mov esi, edi \
      __asm add esi, edx \
      __asm rep movsd \
      __asm pop edi \
      __asm pop esi \
      \
      /* Push an extra argument through which the callback can signal \
       * that it doesn't want to carry on but return to caller. \
       */ \
      __asm push TRUE \
      \
      /* Call our C callback */ \
      __asm call name##_called \
      \
      __asm pop edx \
      __asm cmp edx, FALSE \
      __asm jnz CARRY_ON /* should we carry on or return to caller? */ \
      \
      /* Return to caller */ \
      __asm mov ecx, [esp] /* store return address */ \
      __asm mov edx, args_size \
      __asm add edx, 4 \
      __asm shl edx, 1 \
      __asm add esp, edx /* clear off both copies of (retaddr + args) */ \
      __asm jmp ecx      /* return */ \
      \
      __asm CARRY_ON: \
      /* Overwrite return address so that we can trap the return. */ \
      __asm mov ecx, name##_done_proxy \
      __asm mov dword ptr [esp], ecx \
      \
      /* Set up stack frame (we need to do this because we've */ \
      /* overwritten this code in the start of the original function) */ \
      __asm push ebp \
      __asm mov ebp, esp \
      \
      /* Continue */ \
      __asm mov ecx, [name##_start] \
      __asm add ecx, 5 \
      __asm jmp ecx \
    } \
  }

#define HOOK_FUNCTION(handle, name) HOOK_FUNCTION_BY_ALIAS(handle, name, name)

#define HOOK_FUNCTION_BY_ALIAS(handle, name, alias) \
  { \
    unsigned char signature[5] = { \
      0x8B, 0xFF, /* mov edi, edi */ \
      0x55,       /* push ebp */ \
      0x8B, 0xEC  /* mov ebp, esp */ \
    }; \
    \
    name##_start = GetProcAddress(handle, #name); \
    if (name##_start != NULL) \
    { \
      if (address_has_bytes(name##_start, signature, sizeof(signature))) \
      { \
        write_jmp_instruction_to_addr(name##_start, name##_hook); \
      } \
      else \
      { \
        MessageBox(0, "Signature of " #name " is incompatible", \
                      "oSpy", MB_ICONWARNING | MB_OK); \
      } \
    } \
    else \
    { \
      MessageBox(0, "GetProcAddress of " #name " failed", \
                    "oSpy", MB_ICONWARNING | MB_OK); \
    } \
  }

#define HOOK_GLUE_EXTENDED(name, args_size) \
  static LPVOID name##_start = NULL; \
  \
  __declspec(naked) static void name##_done_proxy() \
  { \
    __asm { \
	  __asm pushad /* Could be useful to have a copy of the registers after return */ \
	  \
	  /* Place return value first for convenience */ \
	  __asm push eax \
	  \
      /* Copy retaddr to the top of the stack */ \
      __asm sub esp, 4 \
      __asm push ebp \
      __asm mov ebp, [esp+4+4+4+32+32+4] \
      __asm mov [esp+4], ebp \
      __asm pop ebp \
      \
      /* Call our C callback */ \
      __asm jmp name##_done \
    } \
  } \
  \
  __declspec(naked) static void name##_hook() \
  { \
    __asm { \
	  /* The backtrace address could be useful. */ \
	  __asm sub esp, 4 \
	  __asm push ebp \
	  __asm lea ebp, [esp+4+4] \
	  __asm mov [esp+4], ebp \
	  __asm pop ebp \
	  \
	  /* Marshalled as a CpuContext struct, and also very useful \
       * to make sure that all registers are preserved. */ \
	  __asm pushad \
	  \
      /* Push an extra argument through which the callback can signal \
       * that it doesn't want to carry on but return to caller. \
       */ \
      __asm push TRUE \
      \
      /* Call our C callback */ \
      __asm call name##_called \
      \
      __asm pop edx \
      __asm cmp edx, FALSE \
	  \
	  __asm popad /* restore the registers again */ \
	  \
      __asm jnz CARRY_ON /* should we carry on or return to caller? */ \
      \
      /* Return to caller */ \
      __asm ret args_size \
      \
      __asm CARRY_ON: \
	  __asm pushad /* store all registers to use as a CpuContext to the second callback */ \
	  \
      /* Make a copy of retaddr + args onto the top of the stack */ \
      __asm sub esp, (args_size + 4) \
	  __asm push ecx \
      __asm push esi \
      __asm push edi \
      __asm lea edi, [12 + esp] \
      __asm mov esi, edi \
      __asm add esi, (args_size + 4 + 32 + 4) \
      __asm cld \
	  __asm lea ecx, (ds:args_size + 4) / 4 \
      __asm rep movsd \
      __asm pop edi \
      __asm pop esi \
	  __asm pop ecx \
	  \
      /* Replace the return address on the copy so that we can trap the return. */ \
	  __asm add esp, 4 \
      __asm push name##_done_proxy \
      \
      /* Set up stack frame (we need to do this because we've */ \
      /* overwritten this code in the start of the original function) */ \
      __asm push ebp \
      __asm mov ebp, esp \
      \
      /* Continue */ \
	  __asm push [name##_start] \
	  __asm add [esp], 5 \
	  __asm ret \
    } \
  }

#define HOOK_GLUE_SPECIAL(name, args_size) \
    static LPVOID name##_start; \
    static int name##_locals_size; \
    static DWORD name##_cookie_offset; \
    \
    __declspec(naked) static void name##_done_proxy() \
    { \
        __asm { \
            /* Store return value (eax) between return address and first argument */ \
            __asm sub esp, 4 \
            __asm push ebp \
            __asm mov ebp, [esp+8] \
            __asm mov [esp+4], ebp \
            __asm mov [esp+8], eax \
            __asm pop ebp \
            \
            /* Call our C callback */ \
            __asm jmp name##_done \
        } \
    } \
    \
    __declspec(naked) static void name##_hook() \
    { \
        __asm { \
            /* Calculate size of retaddr + args */ \
            __asm mov edx, 4            /* edx contains the total size of */ \
            __asm add edx, args_size    /* return address + all arguments. */ \
            \
            __asm mov ecx, edx          /* ecx contains edx divided by 4 */ \
            __asm shr ecx, 2            /* to speed up the copying. */ \
            \
            /* Make a copy of retaddr + args onto the top of the stack */ \
            __asm sub esp, edx \
            __asm push esi \
            __asm push edi \
            __asm cld \
            __asm lea edi, [8 + esp] \
            __asm mov esi, edi \
            __asm add esi, edx \
            __asm rep movsd \
            __asm pop edi \
            __asm pop esi \
            \
            /* Push an extra argument through which the callback can signal \
            * that it doesn't want to carry on but return to caller. \
            */ \
            __asm push TRUE \
            \
            /* Call our C callback */ \
            __asm call name##_called \
            \
            __asm pop edx \
            __asm cmp edx, FALSE \
            __asm jnz CARRY_ON /* should we carry on or return to caller? */ \
            \
            /* Return to caller */ \
            __asm mov ecx, [esp] /* store return address */ \
            __asm mov edx, args_size \
            __asm add edx, 4 \
            __asm shl edx, 1 \
            __asm add esp, edx /* clear off both copies of (retaddr + args) */ \
            __asm jmp ecx      /* return */ \
            \
            __asm CARRY_ON: \
            /* Overwrite return address so that we can trap the return. */ \
            __asm mov ecx, name##_done_proxy \
            __asm mov dword ptr [esp], ecx \
            \
            /* Do what the overwritten code at the start of the function did. */ \
            __asm push [name##_locals_size] \
            __asm push [name##_cookie_offset] \
            \
            /* Continue */ \
            __asm mov ecx, [name##_start] \
            __asm add ecx, 7 \
            __asm jmp ecx \
        } \
    }

#define HOOK_FUNCTION_SPECIAL(handle, name) HOOK_FUNCTION_SPECIAL_BY_ALIAS(handle, name, name)

#define HOOK_FUNCTION_SPECIAL_BY_ALIAS(handle, name, alias) \
    { \
        name##_start = GetProcAddress(handle, #name); \
        if (name##_start != NULL) \
        { \
            unsigned char *p = (unsigned char *) name##_start; \
            if (p[0] == 0x6A && p[2] == 0x68 && p[7] == 0xE8) \
            { \
                name##_locals_size = p[1]; \
                name##_cookie_offset = *((DWORD *) (p + 3)); \
                write_jmp_instruction_to_addr(name##_start, name##_hook); \
            } \
            else \
            { \
                MessageBox(0, "Signature of " #name " is incompatible", \
                           "oSpy", MB_ICONWARNING | MB_OK); \
            } \
        } \
        else \
        { \
            MessageBox(0, "GetProcAddress of " #name " failed", \
                       "oSpy", MB_ICONWARNING | MB_OK); \
        } \
    }

void write_byte_to_addr(LPVOID lpAddr, BYTE b);
void write_dword_to_addr(LPVOID lpAddr, DWORD dw);
BOOL write_jmp_instruction_to_addr(LPVOID lpOrgProc, LPVOID lpNewProc);
BOOL find_signature(const FunctionSignature *sig, LPVOID *address, char **error);
BOOL find_signature_in_module(const FunctionSignature *sig, const char *module_name, LPVOID *address, char **error);
BOOL override_function_by_signature(const FunctionSignature *sig, LPVOID replacement, LPVOID *patched_address, char **error);
BOOL override_function_by_signature_in_module(const FunctionSignature *sig, const char *module_name, LPVOID replacement, LPVOID *patched_address, char **error);