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
#include <fstream>

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
DWORD CUtil::m_lowestAddress = 0xFFFFFFFF;
DWORD CUtil::m_highestAddress = 0;

void
CUtil::Init()
{
    InitializeCriticalSection(&m_cs);

    {
        HMODULE ntMod = GetModuleHandle(_T("ntdll.dll"));
        _ASSERT(ntMod != NULL);

        m_rtlIpv4AddressToStringImpl = reinterpret_cast<RtlIpv4AddressToStringFunc>(
            GetProcAddress(ntMod, "RtlIpv4AddressToString"));
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
    if (FALSE/*m_rtlIpv4AddressToStringImpl != NULL*/)
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

    m_lowestAddress = 0xFFFFFFFF;
    m_highestAddress = 0;

    m_modules.clear();

    HANDLE process = GetCurrentProcess();

    HMODULE modules[256];
    DWORD bytes_needed;

    if (EnumProcessModules(process, (HMODULE *) &modules,
                           sizeof(modules), &bytes_needed) == 0)
    {
        goto DONE;
    }

    if (bytes_needed > sizeof(modules))
        bytes_needed = sizeof(modules);

    for (unsigned int i = 0; i < bytes_needed / sizeof(HMODULE); i++)
    {
        WCHAR filename[MAX_PATH + 1];
        char basename[128];
        MODULEINFO mi;

        if (GetModuleFileNameExW(process, modules[i], filename, MAX_PATH) != 0 &&
            GetModuleBaseNameA(process, modules[i], basename, sizeof(basename)) != 0 &&
            GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
        {
            filename[MAX_PATH] = 0;

            OModuleInfo modInfo;
            modInfo.name = basename;

            modInfo.preferredStartAddress = FindPreferredImageBaseOf(filename);
            modInfo.startAddress = (DWORD) mi.lpBaseOfDll;
            modInfo.endAddress = (DWORD) mi.lpBaseOfDll + mi.SizeOfImage - 1;
            m_modules[basename] = modInfo;

            if (modInfo.startAddress < m_lowestAddress)
                m_lowestAddress = modInfo.startAddress;
            if (modInfo.endAddress > m_highestAddress)
                m_highestAddress = modInfo.endAddress;
        }
    }

DONE:
    LeaveCriticalSection(&m_cs);
}

DWORD
CUtil::FindPreferredImageBaseOf(const WCHAR *filename)
{
    std::ifstream file;
    file.open(filename, ios::in | ios::binary);
    assert(file.is_open());

    LONG offset;
    file.seekg(offsetof(IMAGE_DOS_HEADER, e_lfanew));
    file.read(reinterpret_cast<char *>(&offset), sizeof(offset));

    DWORD imageBase;
    file.seekg(offset + offsetof(IMAGE_NT_HEADERS, OptionalHeader) + offsetof(IMAGE_OPTIONAL_HEADER, ImageBase));
    file.read(reinterpret_cast<char *>(&imageBase), sizeof(imageBase));

    file.close();

    return imageBase;
}

OString
CUtil::GetModuleNameForAddress(DWORD address)
{
    OString result = "";

    EnterCriticalSection(&m_cs);

    OModuleInfo *mi = CUtil::GetModuleInfoForAddress(address);
    if (mi != NULL)
        result = mi->name.c_str();

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

bool
CUtil::AddressIsWithinExecutableModule(DWORD address)
{
    bool result;

    EnterCriticalSection(&m_cs);

    result = (address >= m_lowestAddress && address <= m_highestAddress);

    LeaveCriticalSection(&m_cs);

    return result;
}

#define OPCODE_CALL_NEAR_RELATIVE     0xE8
#define OPCODE_CALL_NEAR_ABS_INDIRECT 0xFF

OTString
CUtil::CreateBackTrace(void *address)
{
    OTStringStream s;
    int count = 0;
    DWORD *p = (DWORD *) address;

    EnterCriticalSection(&m_cs);

    for (; count < 8 && (char *) p < (char *) address + 16384; p++)
    {
        if (IsBadReadPtr(p, 4))
            break;

        DWORD value = *p;

        if (value >= m_lowestAddress && value <= m_highestAddress)
        {
            bool isRetAddr = false;
            unsigned char *codeAddr = (unsigned char *) value;
            unsigned char *p1 = codeAddr - 5;
            unsigned char *p2 = codeAddr - 6;
            unsigned char *p3 = codeAddr - 3;

            // FIXME: add the other CALL variations
            if ((!IsBadCodePtr((FARPROC) p1) && *p1 == OPCODE_CALL_NEAR_RELATIVE) ||
                (!IsBadCodePtr((FARPROC) p2) && *p2 == OPCODE_CALL_NEAR_ABS_INDIRECT) ||
                (!IsBadCodePtr((FARPROC) p3) && *p3 == OPCODE_CALL_NEAR_ABS_INDIRECT))
            {
                isRetAddr = true;
            }

            if (isRetAddr)
            {
                OModuleInfo *mi = GetModuleInfoForAddress(value);

                if (mi != NULL)
                {
                    if (mi->name == "oSpyAgent.dll")
                        break;

                    DWORD canonicalAddress = mi->preferredStartAddress + (value - mi->startAddress);

                    if (count > 0)
                        s << _T("\n");

                    s << mi->name.c_str() << _T("::0x") << hex << canonicalAddress;

                    count++;
                }
            }
        }
    }

    LeaveCriticalSection(&m_cs);

    return s.str();
}

OModuleInfo *
CUtil::GetModuleInfoForAddress(DWORD address)
{
    OMap<OICString, OModuleInfo>::Type::iterator it;
    for (it = m_modules.begin(); it != m_modules.end(); it++)
    {
        OModuleInfo &mi = (*it).second;

        if (address >= mi.startAddress && address <= mi.endAddress)
        {
            return &mi;
        }
    }

    return NULL;
}

#pragma managed(pop)
