//
// Copyright (C) 2009  Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

#include "StdAfx.hpp"

#using <System.dll>
#using <EasyHook.dll>
#using <oSpy.exe>
#using <System.Windows.Forms.dll>

#include <msclr\lock.h>

#include <udis86.h>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Diagnostics;
using namespace System::Text;
using namespace EasyHook;

namespace oScoutAgent
{
    public interface class ICodeAllocator
    {
    public:
        BYTE *Alloc(DWORD size);
    };

    private ref class CodePage
    {
    public:
        CodePage()
        {
            SYSTEM_INFO si;
            GetSystemInfo(&si);

            startAddress = static_cast<BYTE *>(VirtualAlloc(NULL, si.dwPageSize, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_WRITECOPY));
            endAddress = startAddress + si.dwPageSize;
            offset = startAddress;
        }

        BYTE *TryReserve(DWORD numBytes)
        {
            if (offset + numBytes > endAddress)
                return NULL;
            BYTE *result = offset;
            offset += numBytes;
            return result;
        }

    private:
        BYTE *startAddress;
        BYTE *endAddress;
        BYTE *offset;
    };

    public ref class DllFunction
    {
    public:
        DllFunction(String ^name, void *address, ICodeAllocator ^allocator)
            : allocator(allocator)
        {
            Name = name;
            Address = address;
        }

        void PrepareHook()
        {
            DetermineRelocationSize();

            DWORD trampolineSize = 5 + relocSize;

            BYTE *trampoline = allocator->Alloc(trampolineSize);
        }

        property String ^Name;
        property void *Address;

    private:
        void DetermineRelocationSize()
        {
            DWORD codeSize = 5; // 1 byte JMP + 4 byte offset

            ud_t obj;
            ud_init(&obj);
            ud_set_input_buffer(&obj, static_cast<uint8_t *>(Address), 4096);
            ud_set_mode(&obj, 32);

            relocSize = 0;
            while (relocSize < codeSize)
            {
                DWORD size = ud_disassemble(&obj);
                if (size == 0)
                    throw gcnew ArgumentException("Failed to disassemble instruction");

                relocSize += size;
            }
        }

        DWORD relocSize;
        ICodeAllocator ^allocator;
    };

    public ref class Controller : public IEntryPoint, ICodeAllocator
    {
    public:
        Controller(RemoteHooking::IContext ^context, String ^channelName, array<oSpy::Capture::SoftwallRule ^> ^softwallRules)
        {
            String ^url = "ipc://" + channelName + "/" + channelName;
            manager = dynamic_cast<oSpy::Capture::IManager ^>(Activator::GetObject(oSpy::Capture::IManager::typeid, url));
        }

        // IEntryPoint
        void Run(RemoteHooking::IContext ^context, String ^channelName, array<oSpy::Capture::SoftwallRule ^> ^softwallRules)
        {
            RemoteHooking::WakeUpProcess();

            DWORD myPid = GetCurrentProcessId();

            try
            {
                manager->Ping(myPid);

                for each (ProcessModule ^mod in Process::GetCurrentProcess()->Modules)
                {
                    for each (DllFunction ^func in EnumerateModuleExports(mod))
                    {
                        func->PrepareHook();
                    }
                }
            }
            catch (Exception ^ex)
            {
#ifdef _DEBUG
                pin_ptr<const wchar_t> str = PtrToStringChars(ex->Message);
                MessageBox(NULL, str, _T("Error"), MB_OK | MB_ICONERROR);
#else
                (void) ex;
#endif
            }
        }

        // ICodeAllocator
        virtual BYTE *Alloc(DWORD size)
        {
            for each (CodePage ^cp in codePages)
            {
                BYTE * result = cp->TryReserve(size);
                if (result != NULL)
                    return result;
            }

            CodePage ^cp = gcnew CodePage();
            codePages.Add(cp);
            BYTE *result = cp->TryReserve(size);
            if (result == NULL)
                throw gcnew ArgumentException("No single allocations may exceed page size");
            return result;
        }

    private:
        array<DllFunction ^> ^EnumerateModuleExports(ProcessModule ^mod)
        {
            List<DllFunction ^> result;

            BYTE *modBase = static_cast<BYTE *>(mod->BaseAddress.ToPointer());
            IMAGE_DOS_HEADER *dosHdr = reinterpret_cast<IMAGE_DOS_HEADER *>(modBase);
            IMAGE_NT_HEADERS *ntHdrs = reinterpret_cast<IMAGE_NT_HEADERS *>(&modBase[dosHdr->e_lfanew]);
            IMAGE_EXPORT_DIRECTORY *exp = reinterpret_cast<IMAGE_EXPORT_DIRECTORY *>(&modBase[ntHdrs->OptionalHeader.DataDirectory->VirtualAddress]);
            BYTE *expBegin = modBase + ntHdrs->OptionalHeader.DataDirectory->VirtualAddress;
            BYTE *expEnd = expBegin + ntHdrs->OptionalHeader.DataDirectory->Size - 1;
            if (exp->AddressOfNames != 0)
            {
                DWORD *nameRvas = reinterpret_cast<DWORD *>(&modBase[exp->AddressOfNames]);
                WORD *ordRvas = reinterpret_cast<WORD *>(&modBase[exp->AddressOfNameOrdinals]);
                DWORD *funcRvas = reinterpret_cast<DWORD *>(&modBase[exp->AddressOfFunctions]);
                for (DWORD index = 0; index < exp->NumberOfNames; index++)
                {
                    DWORD funcRva = funcRvas[ordRvas[index]];
                    BYTE *funcAddress = &modBase[funcRva];
                    if (funcAddress < expBegin || funcAddress > expEnd)
                    {
                        String ^funcName = gcnew System::String(reinterpret_cast<char *>(&modBase[nameRvas[index]]));
                        DllFunction ^func = gcnew DllFunction(funcName, funcAddress, this);
                        func->PrepareHook();
                        result.Add(func);
                    }
                }
            }

            return result.ToArray();
        }

        oSpy::Capture::IManager ^manager;
        List<CodePage ^> codePages;
    };
}
