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
#include <psapi.h>
#include <shlwapi.h>

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

Util::Util()
    : m_asciiFuncSpec(NULL),
      m_uniFuncSpec(NULL),
      m_mod(NULL),
      m_asciiFunc(NULL),
      m_uniFunc(NULL),
      m_lowestAddress(0xFFFFFFFF),
      m_highestAddress(0)
{
}

Util *
Util::Instance()
{
    static Util *util = NULL;

    if (util == NULL)
    {
        util = new Util();
    }

    return util;
}

void
Util::Initialize()
{
	InitializeCriticalSection(&m_cs);

	char buf[_MAX_PATH];
	if (GetModuleBaseNameA(GetCurrentProcess(), NULL, buf, sizeof(buf)) > 0)
	{
		m_processName = buf;
	}

    m_asciiFuncSpec = new FunctionSpec("LoadLibraryA");
    m_asciiFuncSpec->SetHandler(OnLoadLibrary);

    m_uniFuncSpec = new FunctionSpec("LoadLibraryW");
    m_uniFuncSpec->SetHandler(OnLoadLibrary);

    m_mod = new DllModule("kernel32.dll");
    m_asciiFunc = new DllFunction(m_mod, m_asciiFuncSpec);
    m_uniFunc = new DllFunction(m_mod, m_uniFuncSpec);

    m_asciiFunc->Hook();
    m_uniFunc->Hook();

	UpdateModuleList();
}

void
Util::UnInitialize()
{
    m_modules.clear();

    if (m_uniFunc != NULL)
    {
        m_uniFunc->UnHook();
        delete m_uniFunc;
    }

    if (m_asciiFunc != NULL)
    {
        m_asciiFunc->UnHook();
        delete m_asciiFunc;
    }

    if (m_mod != NULL)
        delete m_mod;

    if (m_uniFuncSpec != NULL)
        delete m_uniFuncSpec;

    if (m_asciiFuncSpec != NULL)
        delete m_asciiFuncSpec;

    m_processName = "";

    m_lowestAddress = 0xFFFFFFFF;
    m_highestAddress = 0;
}

bool
Util::OnLoadLibrary(FunctionCall *call)
{
    if (call->GetState() == FUNCTION_CALL_LEAVING)
    {
        Instance()->UpdateModuleList();
    }

    return true;
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

                modInfo.handle = modules[i];
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

OModuleInfo
Util::GetModuleInfo(const OICString &name)
{
    OModuleInfo mi;

    EnterCriticalSection(&m_cs);

    mi = m_modules[name];

    LeaveCriticalSection(&m_cs);

    return mi;
}

OModuleInfo
Util::GetModuleInfo(void *address)
{
    OModuleInfo result;

	EnterCriticalSection(&m_cs);

    bool found = false;
	OModuleInfo *mi = GetModuleInfoForAddress(reinterpret_cast<DWORD>(address));
	if (mi != NULL)
    {
        found = true;
		result = *mi;
    }

	LeaveCriticalSection(&m_cs);

    if (!found)
        throw Error("No module found");

    return result;
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
Util::AddressIsWithinAnyModule(DWORD address)
{
	bool result;

	EnterCriticalSection(&m_cs);

	result = (address >= m_lowestAddress && address <= m_highestAddress);

	LeaveCriticalSection(&m_cs);

	return result;
}

bool
Util::AddressIsWithinModule(DWORD address, const OICString &moduleName)
{
    bool result = false;

	EnterCriticalSection(&m_cs);

	OModuleInfo *mi = GetModuleInfoForAddress(address);
    if (mi != NULL)
    {
        if (mi->name == moduleName)
            result = true;
    }

	LeaveCriticalSection(&m_cs);

    return result;
}

OString
Util::GetDirectory(const OModuleInfo &mi)
{
    // FIXME: should really use unicode strings

    char buf[MAX_PATH];
    if (GetModuleFileNameA(mi.handle, buf, sizeof(buf)) == 0)
        throw Error("GetModuleFileNameA failed");

    PathRemoveFileSpecA(buf);

    return buf;
}

#define OPCODE_CALL_NEAR_RELATIVE     0xE8
#define OPCODE_CALL_NEAR_ABS_INDIRECT 0xFF

Logging::Node *
Util::CreateBacktraceNode(void *address)
{
    Logging::Element *btNode = NULL;

	int count = 0;
	DWORD *p = reinterpret_cast<DWORD *>(address);

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
                    // FIXME
					if (mi->name == "oSpyAgent.dll")
						break;

					DWORD canonicalAddress = mi->preferredStartAddress + (value - mi->startAddress);

                    Logging::TextNode *entry = new Logging::TextNode("entry");
                    entry->AddField("moduleName", mi->name.c_str());

                    OOStringStream ss;
                    ss << "0x" << hex << canonicalAddress;

                    entry->SetText(ss.str());

                    if (btNode == NULL)
                        btNode = new Logging::Element("backtrace");
                    btNode->AppendChild(entry);

					count++;
				}
			}
		}
	}

	LeaveCriticalSection(&m_cs);

	return btNode;
}

OString
Util::CreateBacktrace(void *address)
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

} // namespace InterceptPP