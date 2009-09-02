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
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Text;
using namespace EasyHook;

namespace oScoutAgent
{
    #define OPCODE_INT3 (0xCC)

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

            startAddress = static_cast<BYTE *>(VirtualAlloc(NULL, si.dwPageSize, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE));
            memset(startAddress, OPCODE_INT3, si.dwPageSize);
            endAddress = startAddress + si.dwPageSize;
            offset = startAddress;
        }

        BYTE *TryReserve(DWORD numBytes)
        {
            if (offset + numBytes > endAddress)
                return NULL;
            BYTE *result = offset;
            offset += numBytes;

            const DWORD align = 16;
            if (reinterpret_cast<size_t>(offset) % align != 0)
            {
                offset = reinterpret_cast<BYTE *>((reinterpret_cast<size_t>(offset) + (align - 1)) & ~(align - 1));
                if (offset > endAddress)
                    offset = endAddress;
            }

            return result;
        }

    private:
        BYTE *startAddress;
        BYTE *endAddress;
        BYTE *offset;
    };

    private class CodeWriter
    {
    public:
        CodeWriter(void *data)
            : dataStart(static_cast<BYTE *>(data)), dataCur(static_cast<BYTE *>(data))
        {
        }

        CodeWriter(BYTE *data)
            : dataStart(data), dataCur(data)
        {
        }

        void WriteInt8(char b)
        {
            *reinterpret_cast<char *>(dataCur) = b;
            dataCur++;
        }

        void WriteUInt8(BYTE b)
        {
            *dataCur = b;
            dataCur++;
        }

        void WriteInt32(int i)
        {
            *reinterpret_cast<int *>(dataCur) = i;
            dataCur += sizeof(i);
        }

        void WriteUInt32(DWORD dw)
        {
            *reinterpret_cast<DWORD *>(dataCur) = dw;
            dataCur += sizeof(dw);
        }

        void WriteBytes(void *bytes, DWORD numBytes)
        {
            memcpy(dataCur, bytes, numBytes);
            dataCur += numBytes;
        }

        void WriteJump(void *target)
        {
            int distance = reinterpret_cast<int>(target) - reinterpret_cast<int>(dataCur + 5);
            WriteUInt8(0xE9);
            WriteInt32(distance);
        }

    private:
        BYTE *dataStart;
        BYTE *dataCur;
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
            relocationSize = DetermineRelocationSize(Address);
            if (relocationSize == 0)
                return;

            const DWORD codeSize = 2 + 6 + 5 + 2 + 7 + 2;
            const DWORD jumpSize = 5;

            trampolineSize = codeSize + relocationSize + jumpSize;

            const DWORD align = 16;
            if (trampolineSize % align != 0)
            {
                trampolineSize = (trampolineSize + (align - 1)) & ~(align - 1);
            }

            trampolineSize += sizeof(DWORD);

            trampoline = allocator->Alloc(trampolineSize);
            callCount = reinterpret_cast<DWORD *>(trampoline + trampolineSize - sizeof(DWORD));
            *callCount = 0;

            CodeWriter cw(trampoline);

            // pushfd
            cw.WriteUInt8(0x9C);

            // push eax
            cw.WriteUInt8(0x50);

            // mov eax, [fs:24h]
            cw.WriteUInt8(0x64); cw.WriteUInt8(0xA1);
            cw.WriteUInt32(0x24);

            // cmp eax, <scout-thread-id>
            cw.WriteUInt8(0x3D);
            cw.WriteUInt32(GetCurrentThreadId());

            // jz +n
            char n = 7;
            cw.WriteUInt8(0x74);
            cw.WriteInt8(n);

            // lock inc dword [trampoline + TrampolineHeader.InvocationCount]
            cw.WriteUInt8(0xF0); cw.WriteUInt8(0xFF); cw.WriteUInt8(0x05);
            cw.WriteUInt32(reinterpret_cast<DWORD>(callCount));

            // pop eax
            cw.WriteUInt8(0x58);

            // popfd
            cw.WriteUInt8(0x9D);

            // original instructions
            cw.WriteBytes(Address, relocationSize);
            BYTE *continueAddr = static_cast<BYTE *>(Address) + relocationSize;
            // followed by a JMP back to the next instruction
            cw.WriteJump(continueAddr);
        }

        void ActivateHook()
        {
            // Temporary
            if (relocationSize == 0)
                return;

            DWORD oldProt;
            VirtualProtect(Address, 5, PAGE_EXECUTE_WRITECOPY, &oldProt);

            CodeWriter cw(Address);
            cw.WriteJump(trampoline);

            VirtualProtect(Address, 5, oldProt, &oldProt);
        }

        property String ^Name;
        property void *Address;

    private:
        static DWORD DetermineRelocationSize(void *address)
        {
            DWORD codeSize = 5; // 1 byte JMP + 4 byte offset

            ud_t obj;
            ud_init(&obj);
            ud_set_input_buffer(&obj, static_cast<uint8_t *>(address), 4096);
            ud_set_mode(&obj, 32);

            DWORD relocSize = 0;
            while (relocSize < codeSize)
            {
                DWORD size = ud_disassemble(&obj);
                if (size == 0)
                    throw gcnew ArgumentException("Failed to disassemble instruction");
                else if (!IsRelocatableInstruction(obj.mnemonic))
                    return 0;

                relocSize += size;
            }
            return relocSize;
        }

        static bool IsRelocatableInstruction(enum ud_mnemonic_code insn)
        {
            static const enum ud_mnemonic_code jumpCodes[] =
            {
              UD_Icall,
              UD_Ija,
              UD_Ijae,
              UD_Ijb,
              UD_Ijbe,
              UD_Ijcxz,
              UD_Ijecxz,
              UD_Ijg,
              UD_Ijge,
              UD_Ijl,
              UD_Ijle,
              UD_Ijmp,
              UD_Ijnp,
              UD_Ijns,
              UD_Ijnz,
              UD_Ijo,
              UD_Ijp,
              UD_Ijrcxz,
              UD_Ijs,
              UD_Ijz,
              UD_Iret,
              UD_Iretf
            };

            for (DWORD i = 0; i < sizeof(jumpCodes) / sizeof(jumpCodes[i]); i++)
            {
                if (insn == jumpCodes[i])
                    return false;
            }

            return true;
        }

        ICodeAllocator ^allocator;

        BYTE *trampoline;
        DWORD trampolineSize;
        DWORD relocationSize;

        DWORD *callCount;
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
                        dllFunctions.Add(func);
                    }
                }

                for each (DllFunction ^func in dllFunctions)
                {
                    if (func->Name == "recv" || func->Name == "send")
                    {
                        func->ActivateHook();
                    }
                }

                while (true)
                {
                    Sleep(500);
                    manager->Ping(myPid);
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

            MessageBox(NULL, _T("Now exiting..."), _T("oScoutAgent"), MB_OK | MB_ICONINFORMATION);
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
                        result.Add(func);
                    }
                }
            }

            return result.ToArray();
        }

        oSpy::Capture::IManager ^manager;
        List<CodePage ^> codePages;
        List<DllFunction ^> dllFunctions;
    };
}
