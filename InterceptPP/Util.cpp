//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

#include "Util.h"
#include <psapi.h>
#include <shlwapi.h>

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

Util::Util()
    : m_ansiFuncSpec(NULL),
      m_uniFuncSpec(NULL),
      m_mod(NULL),
      m_ansiFunc(NULL),
      m_uniFunc(NULL),
      m_lowestAddress(0xFFFFFFFF),
      m_highestAddress(0)
{
    m_loadLibraryHandler.Initialize (this, &Util::OnLoadLibrary);
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

    char buf[_MAX_PATH] = { 0, };
    if (GetModuleBaseNameA(GetCurrentProcess(), NULL, buf, sizeof(buf)) > 0)
    {
        m_processName = buf;
    }

    m_ansiFuncSpec = new FunctionSpec("LoadLibraryA");
    m_ansiFuncSpec->SetHandler(&m_loadLibraryHandler);

    m_uniFuncSpec = new FunctionSpec("LoadLibraryW");
    m_uniFuncSpec->SetHandler(&m_loadLibraryHandler);

    m_mod = new DllModule("kernel32.dll");
    m_ansiFunc = new DllFunction(m_mod, m_ansiFuncSpec);
    m_uniFunc = new DllFunction(m_mod, m_uniFuncSpec);

    m_ansiFunc->Hook();
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

    if (m_ansiFunc != NULL)
    {
        m_ansiFunc->UnHook();
        delete m_ansiFunc;
    }

    if (m_mod != NULL)
        delete m_mod;

    if (m_uniFuncSpec != NULL)
        delete m_uniFuncSpec;

    if (m_ansiFuncSpec != NULL)
        delete m_ansiFuncSpec;

    m_processName = "";

    m_lowestAddress = 0xFFFFFFFF;
    m_highestAddress = 0;
}

void
Util::OnLoadLibrary (FunctionCall * call, bool & shouldLog)
{
    if (call->GetState () == FUNCTION_CALL_LEAVING)
    {
        Instance()->UpdateModuleList ();
    }

    shouldLog = false;
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
    DWORD *p = (DWORD *) address;

    for (; count < 8 && (char *) p < (char *) address + 16384; p++)
    {
        if (IsBadReadPtr(p, 4))
            break;

        DWORD value = *p;

        if (value < m_lowestAddress || value > m_highestAddress)
            continue;

        unsigned char *codeAddr = (unsigned char *) value;
        if (IsBadReadPtr(reinterpret_cast<FARPROC>(codeAddr - 6), 6))
            continue;

        if (*(codeAddr - 5) == OPCODE_CALL_NEAR_RELATIVE ||
            *(codeAddr - 6) == OPCODE_CALL_NEAR_ABS_INDIRECT ||
            *(codeAddr - 3) == OPCODE_CALL_NEAR_ABS_INDIRECT ||
            *(codeAddr - 2) == OPCODE_CALL_NEAR_ABS_INDIRECT)
        {
            EnterCriticalSection(&m_cs);

            OModuleInfo *mi = GetModuleInfoForAddress(value);

            if (mi != NULL && mi->name != "oSpyAgent.dll")
            {
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

            LeaveCriticalSection(&m_cs);
        }
    }

    return btNode;
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
