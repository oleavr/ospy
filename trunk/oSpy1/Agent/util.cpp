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

static bool initialized = FALSE;
static char cur_process_name[_MAX_PATH];

bool
cur_process_is(const char *name)
{
  if (!initialized)
  {
    initialized = TRUE;

    // Get the current process name
    if (GetModuleBaseName(GetCurrentProcess(), NULL, cur_process_name, _MAX_PATH) == 0)
    {
      message_logger_log_message("DllMain", 0, MESSAGE_CTX_WARNING,
                                 "GetModuleBaseName failed with errno %d",
                                 GetLastError());
      return FALSE;
    }
  }

  return (stricmp(cur_process_name, name) == 0);
}

void
get_module_name_for_address(LPVOID address,
                            char *buf, int buf_size)
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
get_module_base_and_size(const char *module_name, LPVOID *base, DWORD *size, char **error)
{
    HANDLE process;
    HMODULE modules[256];
    DWORD bytes_needed, num_modules;
    unsigned int i;

    process = GetCurrentProcess();

    if (EnumProcessModules(process, (HMODULE *) &modules,
                           sizeof(modules), &bytes_needed) == 0)
    {
        if (error)
            *error = sspy_strdup("EnumProcessModules failed");
        return FALSE;
    }

    if (bytes_needed > sizeof(modules))
        bytes_needed = sizeof(modules);

    num_modules = bytes_needed / sizeof(HMODULE);

    for (i = 0; i < num_modules; i++)
    {
        MODULEINFO mi;

        if (GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
        {
            char buf[32];
            LPVOID start, end;

            start = mi.lpBaseOfDll;
            end = (char *) start + mi.SizeOfImage;

            if (GetModuleBaseName(process, modules[i], buf, 32) == 0)
            {
                if (error)
                    *error = sspy_strdup("GetModuleBaseName failed");
                return FALSE;
            }

            if (stricmp(buf, module_name) == 0)
            {
                *base = mi.lpBaseOfDll;
                *size = mi.SizeOfImage;

                return TRUE;
            }
        }
    }

    if (error)
        *error = sspy_strdup("module not found");
    return FALSE;
}

void get_process_name(char *name, int len)
{
  WCHAR path_buffer[_MAX_PATH];
  WCHAR drive[_MAX_DRIVE];
  WCHAR dir[_MAX_DIR];
  WCHAR fname[_MAX_FNAME];
  WCHAR ext[_MAX_EXT];

  GetModuleFileNameW(NULL, path_buffer, _MAX_PATH);

  _wsplitpath_s(path_buffer, drive, _MAX_DRIVE, dir, _MAX_DIR,
                fname, _MAX_FNAME, ext, _MAX_EXT);

  wsprintf(name, "%S%S", fname, ext);
}

BOOL address_has_bytes(LPVOID address, unsigned char *buf, int len)
{
  unsigned char *func_bytes = (unsigned char *) address;
  int i;

  for (i = 0; i < len; i++)
  {
    if (func_bytes[i] != buf[i])
    {
      message_logger_log_message("address_has_bytes", 0, MESSAGE_CTX_ERROR,
                                 "Signature mismatch on index %d, expected %d got %d",
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
OString CUtil::m_processName = "";
OMap<OICString, OModuleInfo>::Type CUtil::m_modules;
DWORD CUtil::m_lowestAddress = 0xFFFFFFFF;
DWORD CUtil::m_highestAddress = 0;

void
CUtil::Init()
{
	InitializeCriticalSection(&m_cs);

	char buf[_MAX_PATH];
	if (GetModuleBaseNameA(NULL, NULL, buf, sizeof(buf)) > 0)
	{
		m_processName = buf;
	}

	HMODULE h = LoadLibrary("kernel32.dll");
    if (h != NULL)
	{
		HOOK_FUNCTION(h, LoadLibraryA);
		HOOK_FUNCTION(h, LoadLibraryW);
	}

	UpdateModuleList();
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
		char buf[128];

		if (GetModuleBaseNameA(process, modules[i], buf, sizeof(buf)) != 0)
		{
			MODULEINFO mi;
			if (GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
			{
				OModuleInfo modInfo;
				modInfo.name = buf;

				IMAGE_DOS_HEADER *dosHeader = (IMAGE_DOS_HEADER *) modules[i];
				IMAGE_NT_HEADERS *peHeader = (IMAGE_NT_HEADERS *) ((char *) modules[i] + dosHeader->e_lfanew);

				modInfo.preferredStartAddress = peHeader->OptionalHeader.ImageBase;
				modInfo.startAddress = (DWORD) mi.lpBaseOfDll;
				modInfo.endAddress = (DWORD) mi.lpBaseOfDll + mi.SizeOfImage - 1;
				m_modules[buf] = modInfo;

				if (modInfo.startAddress < m_lowestAddress)
					m_lowestAddress = modInfo.startAddress;
				if (modInfo.endAddress > m_highestAddress)
					m_highestAddress = modInfo.endAddress;
			}
		}
    }

DONE:
	LeaveCriticalSection(&m_cs);
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

OString
CUtil::CreateBackTrace(void *address)
{
	OStringStream s;
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
						s << "\n";

					s << mi->name.c_str() << "::0x" << hex << canonicalAddress;

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
