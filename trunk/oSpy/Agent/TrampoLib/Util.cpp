//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#include "stdafx.h"
#include "Util.h"
#include "..\hooking.h" // FIXME: yuck, use TrampoLib's hooking instead
#include <psapi.h>

namespace TrampoLib {

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

	Util::UpdateModuleList();

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

	Util::UpdateModuleList();

    SetLastError(err);
    return retval;
}

HOOK_GLUE_INTERRUPTIBLE(LoadLibraryA, (1 * 4))
HOOK_GLUE_INTERRUPTIBLE(LoadLibraryW, (1 * 4))

CRITICAL_SECTION Util::m_cs = { 0, };
OString Util::m_processName = "";
OMap<OICString, OModuleInfo>::Type Util::m_modules;
DWORD Util::m_lowestAddress = 0xFFFFFFFF;
DWORD Util::m_highestAddress = 0;

void
Util::Initialize()
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
Util::UpdateModuleList()
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
Util::GetModuleNameForAddress(DWORD address)
{
	OString result = "";

	EnterCriticalSection(&m_cs);

	OModuleInfo *mi = Util::GetModuleInfoForAddress(address);
	if (mi != NULL)
		result = mi->name.c_str();

	LeaveCriticalSection(&m_cs);

	return result;
}

OModuleInfo
Util::GetModuleInfo(const OICString &name)
{
    OModuleInfo mi;

    EnterCriticalSection(&m_cs);

    mi = m_modules[name];

    LeaveCriticalSection(&m_cs);

    return mi;
}

OVector<OModuleInfo>::Type
Util::GetAllModules()
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
Util::AddressIsWithinExecutableModule(DWORD address)
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
Util::CreateBackTrace(void *address)
{
	OOStringStream s;
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
Util::GetModuleInfoForAddress(DWORD address)
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

} // namespace TrampoLib