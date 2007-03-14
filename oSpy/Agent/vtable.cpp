//
// Copyright (C) 2007  Ole André Vadla Ravnås <oleavr@gmail.com>
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

#include "stdafx.h"
#include "vtable.h"
#include "logging.h"

VTableSpec::VTableSpec(const OString &name, int methodCount)
	: m_name(name), m_methods(methodCount)
{
	for (int i = 0; i < methodCount; i++)
	{
		m_methods[i].Initialize(this, i);
	}
}

void
VMethodSpec::Initialize(VTableSpec *vtable, int index)
{
	m_vtable = vtable;
	m_index = index;

	OStringStream ss;
	ss << "Method_" << index;
	m_name = ss.str();
}

VTable::VTable(VTableSpec *spec, DWORD startOffset)
	: m_spec(spec), m_startOffset(startOffset), m_methods(spec->GetMethodCount())
{
	DWORD *funcs = (DWORD *) startOffset;

	for (int i = 0; i < spec->GetMethodCount(); i++)
	{
		m_methods[i].Initialize(&(*spec)[i], this, funcs[i]);
	}
}

void
VTable::Hook()
{
	VTableSpec *spec = GetSpec();

	DWORD oldProtect;
	VirtualProtect((LPVOID) m_startOffset, spec->GetMethodCount() * sizeof(LPVOID),
		PAGE_READWRITE, &oldProtect);

	DWORD *methods = (DWORD *) m_startOffset;

	for (int i = 0; i < spec->GetMethodCount(); i++)
	{
		methods[i] = (DWORD) m_methods[i].CreateTrampoline();
	}
}

VMethodTrampoline *
VMethod::CreateTrampoline()
{
	VMethodTrampoline *trampoline = new VMethodTrampoline;

	trampoline->CALL_opcode = 0xE8;
	trampoline->CALL_offset = (DWORD) OnEnterProxy - (DWORD) &(trampoline->data);
	trampoline->data = this;

	return trampoline;
}

__declspec(naked) void
VMethod::OnEnterProxy(CpuContext cpuCtx, VMethod *method)
{
	void *retAddr;
	VMethodTrampoline *trampoline;

	__asm {
											// *** We're coming in hot from the modified vtable ***

		sub esp, 4;							//  1. Reserve space for the second argument to this function (VMethod *).
		push eax;							//  2. Don't clobber any registers.
		push ebx;
		mov eax, [esp+8+4];					//  3. Get the trampoline returnaddress, which is the address of the VMethod *
											//     right after the CALL instruction on the trampoline
		mov eax, [eax];						//  4. Dereference VMethod **.
		mov ebx, eax;						//  5. Store the VMethod * in ebx.
		mov eax, [eax+VMethod::m_offset];	//  6. Get the actual method's offset (the method that is about to be called).
		mov [esp+8+4], eax;					//  7. Replace the trampoline return-address with the actual method's offset.
		mov [esp+8+0], ebx;					//  8. Store the VMethod * on the reserved spot so that we can access it from
											//     C++ through the second argument.
		pop ebx;
		pop eax;

		pushad;								//  9. Save all registers and conveniently place them so that they're available
											//     from C++ through the first argument.

		sub esp, 4;							// 10. Padding/fake return address so that ebp+8 refers to the first argument.
		push ebp;							// 11. Standard prolog.
		mov ebp, esp;
		sub esp, __LOCAL_SIZE;

		mov eax, [ebp+8+32+4+4];			// 12. Store the return-address.
		mov [retAddr], eax;
	}

	trampoline = method->OnEnterWrapper(retAddr, &cpuCtx);

	__asm {
											// *** Bounce off to the actual method ***

		mov eax, [trampoline];
		mov [ebp+8+32+4+4], eax;

		mov esp, ebp;						//  1. Standard epilog.
		pop ebp;
		add esp, 4;							//  2. Remove the padding/fake return address (see step 10 above).

		popad;								//  3. Clean up the first argument and restore the registers (see step 9 above).

		add esp, 4;							//  4. Clean up the second argument.
		ret;								//  5. Bounce to the actual method.
	}
}

VMethodTrampoline *
VMethod::OnEnterWrapper(void *retAddr, CpuContext *cpuCtx)
{
	VMethodCall *call = new VMethodCall(this, retAddr, cpuCtx);

	// Set up a trampoline used to trap the return
	VMethodTrampoline *trampoline = new VMethodTrampoline;

	trampoline->CALL_opcode = 0xE8;
	trampoline->CALL_offset = (DWORD) VMethod::OnLeaveProxy - (DWORD) &(trampoline->data);
	trampoline->data = call;

	return trampoline;
}

__declspec(naked) void
VMethod::OnLeaveProxy(CpuContext cpuCtx, VMethodCall *call)
{
	__asm {
											// *** We're coming in hot and the method has just been called ***

		sub esp, 4;							//  1. Reserve space for the second argument to this function (VMethodCall *).
		push eax;							//  2. Don't clobber any registers.
		push ebx;
		mov eax, [esp+8+4];					//  3. Get the trampoline returnaddress, which is the address of the VMethodCall *
											//     right after the CALL instruction on the trampoline
		mov eax, [eax];						//  4. Dereference VMethodCall **.
		mov ebx, eax;						//  5. Store the VMethodCall * in ebx.
		mov eax, [eax+VMethodCall::m_returnAddress];	//  6. Get the return address of the caller.
		mov [esp+8+4], eax;					//  7. Replace the trampoline return-address with the return address of the caller.
		mov [esp+8+0], ebx;					//  8. Store the VMethodCall * on the reserved spot so that we can access it from
											//     C++ through the second argument.
		pop ebx;
		pop eax;

		pushad;								//  9. Save all registers and conveniently place them so that they're available
											//     from C++ through the first argument.

		sub esp, 4;							// 10. Padding/fake return address so that ebp+8 refers to the first argument.
		push ebp;							// 11. Standard prolog.
		mov ebp, esp;
		sub esp, __LOCAL_SIZE;
	}

	call->SetCpuContextLeave(&cpuCtx);
	call->GetMethod()->OnLeave(call);

	__asm {
											// *** Bounce off back to the caller ***

		mov esp, ebp;						//  1. Standard epilog.
		pop ebp;
		add esp, 4;							//  2. Remove the padding/fake return address (see step 10 above).

		popad;								//  3. Clean up the first argument and restore the registers (see step 9 above).

		add esp, 4;							//  4. Clean up the second argument.
		ret;								//  5. Bounce to the caller.
	}
}

void
VMethod::OnEnter(VMethodCall *call)
{
	message_logger_log_message("VTableProxyFunc", 0, MESSAGE_CTX_INFO,
		"Index=%d, Offset=0x%08x, Name=%s",
		m_spec->GetIndex(), GetOffset(), m_spec->GetName().c_str());
}

void
VMethod::OnLeave(VMethodCall *call)
{
	message_logger_log_message("VTableProxyFunc", 0, MESSAGE_CTX_INFO,
		"Index=%d, Offset=0x%08x, Name=%s",
		m_spec->GetIndex(), GetOffset(), m_spec->GetName().c_str());
}
