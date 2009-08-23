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
#include "logging.h"
#include "hooking.h"
#include "util.h"
#include <psapi.h>

#include <assert.h>
#include <algorithm>
#include <fstream>

#include <udis86.h>

#pragma managed(push, off)

static bool initialized = FALSE;
static TCHAR cur_process_name[_MAX_PATH];

bool
cur_process_is(const TCHAR *name)
{
  if (!initialized)
  {
    initialized = TRUE;

    // Get the current process name
    if (GetModuleBaseName(GetCurrentProcess(), NULL, cur_process_name, _MAX_PATH) == 0)
    {
      message_logger_log_message(_T("DllMain"), 0, MESSAGE_CTX_WARNING,
                                 _T("GetModuleBaseName failed with errno %u"),
                                 GetLastError());
      return FALSE;
    }
  }

  return (_tcsicmp(cur_process_name, name) == 0);
}

void
get_module_name_for_address(LPVOID address,
                            TCHAR *buf, int buf_size)
{
    HANDLE process;
    HMODULE modules[256];
    DWORD bytes_needed, num_modules;
    unsigned int i;

    buf[0] = '\0';

    process = GetCurrentProcess();

    if (EnumProcessModules(process, (HMODULE *) &modules,
                           sizeof(modules), &bytes_needed) == 0)
    {
        return;
    }

    if (bytes_needed > sizeof(modules))
        bytes_needed = sizeof(modules);

    num_modules = bytes_needed / sizeof(HMODULE);

    for (i = 0; i < num_modules; i++)
    {
        MODULEINFO mi;

        if (GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
        {
            LPVOID start, end;

            start = mi.lpBaseOfDll;
            end = (char *) start + mi.SizeOfImage;

            if (address >= start && address <= end)
            {
                GetModuleBaseName(process, modules[i], buf, buf_size);
                return;
            }
        }
    }
}

BOOL
get_module_base_and_size(const TCHAR *module_name, LPVOID *base, DWORD *size, char **error)
{
    HMODULE mod = HookManager::Obtain()->OpenLibrary(module_name);
    if (mod == NULL)
    {
        *error = sspy_strdup("module not found");
        return FALSE;
    }

    MODULEINFO mi;
    if (GetModuleInformation(GetCurrentProcess(), mod, &mi, sizeof(mi)) == 0)
    {
        *error = sspy_strdup("GetModuleInformation failed");
        return FALSE;
    }

    *base = mi.lpBaseOfDll;
    *size = mi.SizeOfImage;
    return TRUE;
}

BOOL address_has_bytes(LPVOID address, unsigned char *buf, int len)
{
  unsigned char *func_bytes = (unsigned char *) address;
  int i;

  for (i = 0; i < len; i++)
  {
    if (func_bytes[i] != buf[i])
    {
      message_logger_log_message(_T("address_has_bytes"), 0, MESSAGE_CTX_ERROR,
                                 _T("Signature mismatch on index %d, expected %u got %u"),
                                 i, buf[i], func_bytes[i]);
      return FALSE;
    }
  }

  return TRUE;
}

DWORD
ospy_rand()
{
    LARGE_INTEGER seed;

    QueryPerformanceCounter(&seed);
    srand(seed.LowPart);

    return 1 + rand();
}

CodeRegion::CodeRegion(HMODULE moduleHandle, void *startAddress, DWORD length)
    : ModuleHandle(moduleHandle), StartAddress(startAddress), Length(length)
{
}

static HMODULE __cdecl
LoadLibraryA_called(BOOL carry_on,
                    DWORD ret_addr,
                    LPCSTR lpFileName)
{
    return 0;
}

static HMODULE __stdcall
LoadLibraryA_done(HMODULE retval,
                  LPCSTR lpFileName)
{
    DWORD err = GetLastError();

    CUtil::UpdateModuleList();

    SetLastError(err);
    return retval;
}

static HMODULE __cdecl
LoadLibraryW_called(BOOL carry_on,
                    DWORD ret_addr,
                    LPCWSTR lpFileName)
{
    return 0;
}

static HMODULE __stdcall
LoadLibraryW_done(HMODULE retval,
                  LPCWSTR lpFileName)
{
    DWORD err = GetLastError();

    CUtil::UpdateModuleList();

    SetLastError(err);
    return retval;
}

HOOK_GLUE_INTERRUPTIBLE(LoadLibraryA, (1 * 4))
HOOK_GLUE_INTERRUPTIBLE(LoadLibraryW, (1 * 4))

CRITICAL_SECTION CUtil::m_cs = { 0, };
RtlIpv4AddressToStringFunc CUtil::m_rtlIpv4AddressToStringImpl = NULL;
OTString CUtil::m_processName = _T("");
OMap<OICString, OModuleInfo>::Type CUtil::m_modules;
void *CUtil::m_lowestAddress = reinterpret_cast<void *>(0xFFFFFFFF);
void *CUtil::m_highestAddress = NULL;
OVector<CodeRegion>::Type CUtil::m_codeRegions;

void
CUtil::Init()
{
    InitializeCriticalSection(&m_cs);

    {
        HMODULE ntMod = GetModuleHandle(_T("ntdll.dll"));
        _ASSERT(ntMod != NULL);

        m_rtlIpv4AddressToStringImpl = reinterpret_cast<RtlIpv4AddressToStringFunc>(
            GetProcAddress(ntMod, "RtlIpv4AddressToStringW"));
    }

    {
        TCHAR buf[_MAX_PATH];
        if (GetModuleBaseName(GetCurrentProcess(), NULL, buf, OSPY_N_ELEMENTS(buf)) > 0)
        {
            m_processName = buf;
        }
    }

    HMODULE h = HookManager::Obtain()->OpenLibrary(_T("kernel32.dll"));
    if (h != NULL)
    {
        HOOK_FUNCTION(h, LoadLibraryA);
        HOOK_FUNCTION(h, LoadLibraryW);
    }

    UpdateModuleList();
}

void
CUtil::Ipv4AddressToString(const IN_ADDR *addr,
                           TCHAR *str)
{
    if (m_rtlIpv4AddressToStringImpl != NULL)
    {
        m_rtlIpv4AddressToStringImpl(addr, str);
    }
    else
    {
        const char *ansiStr = inet_ntoa(*addr);
        int result = MultiByteToWideChar(CP_ACP, 0, ansiStr, -1, str, 16);
        _ASSERT(result != 0);
    }
}

void
CUtil::UpdateModuleList()
{
    EnterCriticalSection(&m_cs);

    //
    // Find new modules
    //
    HANDLE process = GetCurrentProcess();

    HMODULE modules[256];
    DWORD bytes_needed;

    if (EnumProcessModules(process, (HMODULE *) &modules,
                           sizeof(modules), &bytes_needed) == 0)
    {
        goto DONE;
    }

    // Make sure we recalculate bounds every time
    m_lowestAddress = reinterpret_cast<void *>(0xFFFFFFFF);
    m_highestAddress = NULL;

    if (bytes_needed > sizeof(modules))
        bytes_needed = sizeof(modules);

    bool moduleAdded = false;

    for (unsigned int i = 0; i < bytes_needed / sizeof(HMODULE); ++i)
    {
        char basename[128];
        if (GetModuleBaseNameA(process, modules[i], basename, sizeof(basename)) == 0)
            continue;

        ModuleMap::iterator it = m_modules.find(basename);
        if (it == m_modules.end())
        {
            WCHAR filename[MAX_PATH + 1];
            MODULEINFO mi;

            if (GetModuleFileNameExW(process, modules[i], filename, MAX_PATH) != 0 &&
                GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
            {
                filename[MAX_PATH] = 0;

                OModuleInfo modInfo;
                modInfo.Name = basename;
                modInfo.Handle = modules[i];

                modInfo.PreferredStartAddress = FindPreferredImageBaseOf(filename);
                modInfo.StartAddress = mi.lpBaseOfDll;
                modInfo.EndAddress = static_cast<BYTE *>(mi.lpBaseOfDll) + mi.SizeOfImage - 1;
                pair<ModuleMap::iterator, bool> insertPair = m_modules.insert(ModulePair(basename, modInfo));
                it = insertPair.first;

                AppendCodeRegionsFoundIn(modInfo);

                moduleAdded = true;
            }
        }

        if (it != m_modules.end())
        {
            // Update bounds
            if (it->second.StartAddress < m_lowestAddress)
                m_lowestAddress = it->second.StartAddress;
            if (it->second.EndAddress > m_highestAddress)
                m_highestAddress = it->second.EndAddress;
        }
    }

    //
    // Remove modules that are no longer around
    //
    for (ModuleMap::iterator it = m_modules.begin(); it != m_modules.end();)
    {
        // The most straight-forward approach here would have been to call
        // GetModuleHandle(), but turns out that doing so results in failure
        // with certain modules, so instead we'll do it the silly way...
        bool stillAround = false;

        for (unsigned int i = 0; i < bytes_needed / sizeof(HMODULE) && !stillAround; ++i)
        {
            if (it->second.Handle == modules[i])
                stillAround = true;
        }

        if (stillAround)
        {
            ++it;
        }
        else
        {
            RemoveCodeRegionsOwnedBy(it->second);
            m_modules.erase(it);
            it = m_modules.begin();
        }
    }

    //
    // Finish off
    //
    if (moduleAdded)
        SortCodeRegions();

DONE:
    LeaveCriticalSection(&m_cs);
}

void *
CUtil::FindPreferredImageBaseOf(const WCHAR *filename)
{
    std::ifstream file;
    file.open(filename, ios::in | ios::binary);
    assert(file.is_open());

    LONG offset;
    file.seekg(offsetof(IMAGE_DOS_HEADER, e_lfanew));
    file.read(reinterpret_cast<char *>(&offset), sizeof(offset));

    void *imageBase;
    file.seekg(offset + offsetof(IMAGE_NT_HEADERS, OptionalHeader) + offsetof(IMAGE_OPTIONAL_HEADER, ImageBase));
    file.read(reinterpret_cast<char *>(&imageBase), sizeof(imageBase));

    file.close();

    return imageBase;
}

OString
CUtil::GetModuleNameForAddress(void *address)
{
    OString result = "";

    EnterCriticalSection(&m_cs);

    OModuleInfo *mi = CUtil::GetModuleInfoForAddress(address);
    if (mi != NULL)
        result = mi->Name.c_str();

    LeaveCriticalSection(&m_cs);

    return result;
}

OVector<OModuleInfo>::Type
CUtil::GetAllModules()
{
    OVector<OModuleInfo>::Type ret;

    EnterCriticalSection(&m_cs);

    OMap<OICString, OModuleInfo>::Type::iterator it;
    for (it = m_modules.begin(); it != m_modules.end(); it++)
    {
        ret.push_back((*it).second);
    }

    LeaveCriticalSection(&m_cs);

    return ret;
}

#define MIN_CALL_INSN_SIZE 3
#define MED_CALL_INSN_SIZE 5
#define MAX_CALL_INSN_SIZE 6

#define CHECK_FOR_CALL_INSTRUCTION_AT(PTR, OFFSET)                                  \
    {                                                                               \
        ud_set_input_buffer(&udObj, PTR - OFFSET, OFFSET);                          \
        insnSize = ud_disassemble(&udObj);                                          \
        isCallInstruction = (insnSize == OFFSET) && (udObj.mnemonic == UD_Icall);   \
    }

OTString
CUtil::CreateBackTrace(void *address)
{
    // Get the top of stack from TIB (http://en.wikipedia.org/wiki/Win32_Thread_Information_Block)
    void *topOfStack;

    __asm
    {
        push dword ptr fs:[4]
        pop [topOfStack]
    }

    // Highly unlikely unless we're dealing with a hostile app
    if (topOfStack < address)
        topOfStack = reinterpret_cast<DWORD *>(static_cast<BYTE *>(address) + 16384);

    OTStringStream s;
    s << hex;
    int count = 0;

    ud_t udObj;
    ud_init(&udObj);
    ud_set_mode(&udObj, 32);

    EnterCriticalSection(&m_cs);

    for (BYTE **p = static_cast<BYTE **>(address); count < 8 && p < topOfStack; ++p)
    {
        BYTE *value = *p;

        if (value < m_lowestAddress || value > m_highestAddress)
            continue;

        if (IsWithinCodeRegion(value - MAX_CALL_INSN_SIZE))
        {
            bool isCallInstruction = true;
            unsigned int insnSize;

            CHECK_FOR_CALL_INSTRUCTION_AT(value, MED_CALL_INSN_SIZE);
            if (!isCallInstruction)
                CHECK_FOR_CALL_INSTRUCTION_AT(value, MAX_CALL_INSN_SIZE);
            if (!isCallInstruction)
                CHECK_FOR_CALL_INSTRUCTION_AT(value, MIN_CALL_INSN_SIZE);

            if (isCallInstruction)
            {
                OModuleInfo *mi = GetModuleInfoForAddress(value);

                if (mi != NULL)
                {
                    DWORD canonicalAddress = reinterpret_cast<DWORD>(static_cast<BYTE *>(mi->PreferredStartAddress) + (value - static_cast<BYTE *>(mi->StartAddress)));

                    if (count > 0)
                        s << _T("\n");

                    s << mi->Name.c_str() << _T("::0x") << canonicalAddress;

                    count++;
                }
            }
        }
    }

    LeaveCriticalSection(&m_cs);

    return s.str();
}

OModuleInfo *
CUtil::GetModuleInfoForAddress(void *address)
{
    OMap<OICString, OModuleInfo>::Type::iterator it;
    for (it = m_modules.begin(); it != m_modules.end(); it++)
    {
        OModuleInfo &mi = (*it).second;

        if (address >= mi.StartAddress && address <= mi.EndAddress)
        {
            return &mi;
        }
    }

    return NULL;
}

void
CUtil::AppendCodeRegionsFoundIn(const OModuleInfo &mi)
{
    BYTE *p = reinterpret_cast<BYTE *>(mi.StartAddress);

    do
    {
        MEMORY_BASIC_INFORMATION mbi;
        if (VirtualQuery(p, &mbi, sizeof(mbi)) != sizeof(mbi))
            break;

        if ((mbi.Protect & (PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)) != 0)
        {
            m_codeRegions.push_back(CodeRegion(mi.Handle, mbi.BaseAddress, mbi.RegionSize));
        }

        p = static_cast<BYTE *>(mbi.BaseAddress) + mbi.RegionSize;
    }
    while (p < reinterpret_cast<BYTE *>(mi.EndAddress));
}

void
CUtil::RemoveCodeRegionsOwnedBy(const OModuleInfo &mi)
{
    for (CodeRegionVector::iterator it = m_codeRegions.begin(); it != m_codeRegions.end();)
    {
        if (it->ModuleHandle == mi.Handle)
            it = m_codeRegions.erase(it);
        else
            ++it;
    }
}

static bool
CodeRegionLessThan(const CodeRegion &a, const CodeRegion &b)
{
    return a.StartAddress < b.StartAddress && (static_cast<BYTE *>(a.StartAddress) + a.Length - 1) < b.StartAddress;
}

void
CUtil::SortCodeRegions()
{
    std::sort(m_codeRegions.begin(), m_codeRegions.end(), CodeRegionLessThan);
}

bool
CUtil::IsWithinCodeRegion(void *address)
{
    CodeRegion region(NULL, address, 1);
    return std::binary_search(m_codeRegions.begin(), m_codeRegions.end(), region, CodeRegionLessThan);
}

#pragma managed(pop)
