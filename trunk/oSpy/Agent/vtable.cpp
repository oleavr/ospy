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

namespace InterceptLib {

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

VTable::VTable(VTableSpec *spec, const OString &name, DWORD startOffset)
	: m_spec(spec), m_name(name), m_startOffset(startOffset), m_methods(spec->GetMethodCount())
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
VMethod::OnEnterProxy(CpuContext cpuCtx, unsigned int unwindSize, VMethodTrampoline *trampoline, void **proxyRet, void **finalRet)
{
	DWORD lastError;
	VMethod *method;
	VMethodTrampoline *nextTrampoline;

	__asm {
											// *** We're coming in hot from the modified vtable through the trampoline ***

		sub esp, 12;						//  1. Reserve space for the last 3 arguments.

		push 16;							//  2. Set unwindSize to the size of the last 4 arguments.

		pushad;								//  3. Save all registers and conveniently place them so that they're available
											//     from C++ through the first argument.

		lea eax, [esp+48+4];				//  4. Set finalRet to point to the final return address.
		mov [esp+48-4], eax;

		lea eax, [esp+48+0];				//  5. Set proxyRet to point to this function's return address.
		mov [esp+48-8], eax;

		mov eax, [eax];						//  6. Set trampoline to point to the start of the trampoline, ie. *proxyRet - 5.
        sub eax, 5;
		mov [esp+48-12], eax;

		sub esp, 4;							//  7. Padding/fake return address so that ebp+8 refers to the first argument.
		push ebp;							//  8. Standard prolog.
		mov ebp, esp;
		sub esp, __LOCAL_SIZE;
	}

	lastError = GetLastError();

	method = static_cast<VMethod *>(trampoline->data);
	nextTrampoline = method->OnEnterWrapper(&cpuCtx, &unwindSize, trampoline, finalRet, &lastError);
	if (nextTrampoline != NULL)
	{
		*proxyRet = reinterpret_cast<void *>(method->GetOffset());
		*finalRet = nextTrampoline;
	}

	SetLastError(lastError);

	__asm {
											// *** Bounce off to the actual method, or straight back to the caller. ***

		mov esp, ebp;						//  1. Standard epilog.
		pop ebp;
		add esp, 4;							//  2. Remove the padding/fake return address (see step 7 above).

		popad;								//  3. Clean up the first argument and restore the registers (see step 3 above).

		add esp, [esp];						//  4. Clean up the remaining arguments (and more if returning straight back).

		ret;
	}
}

VMethodTrampoline *
VMethod::OnEnterWrapper(CpuContext *cpuCtx, unsigned int *unwindSize, VMethodTrampoline *trampoline, void *btAddr, DWORD *lastError)
{
	// Keep track of the method call
	VMethodCall *call = new VMethodCall(this, btAddr, cpuCtx);
	call->SetCpuContextLive(cpuCtx);
	call->SetLastErrorLive(lastError);

	OnEnter(call);

	bool carryOn = call->GetShouldCarryOn();

	VMethodSpec *spec = call->GetMethod()->GetSpec();
	CallingConvention conv = spec->GetCallingConvention();
	if (conv == CALLING_CONV_UNKNOWN ||
		(conv != CALLING_CONV_CDECL && spec->GetArgsSize() == VMETHOD_ARGS_SIZE_UNKNOWN))
	{
		// TODO: log a warning here
		carryOn = true;
	}

	if (carryOn)
	{
		// Set up a trampoline used to trap the return
		VMethodTrampoline *retTrampoline = new VMethodTrampoline;

		retTrampoline->CALL_opcode = 0xE8;
		retTrampoline->CALL_offset = (DWORD) VMethod::OnLeaveProxy - (DWORD) &(retTrampoline->data);
		retTrampoline->data = call;

		return retTrampoline;
	}
	else
	{	
		// Clear off the proxy return address.
		*unwindSize += sizeof(void *);

		if (conv != CALLING_CONV_CDECL)
		{
			*unwindSize += spec->GetArgsSize();

			void **retAddr = reinterpret_cast<void **>(static_cast<char *>(btAddr) + spec->GetArgsSize());
			*retAddr = call->GetReturnAddress();
		}
	}

	delete call;
	return NULL;
}

__declspec(naked) void
VMethod::OnLeaveProxy(CpuContext cpuCtx, VMethodTrampoline *trampoline)
{
	VMethodCall *call;

	__asm {
											// *** We're coming in hot and the method has just been called ***

		sub esp, 4;							//  1. Reserve space for the second argument to this function (VMethodTrampoline *).
		push eax;
		push ebx;
		mov eax, [esp+8+4];					//  2. Get the trampoline returnaddress, which is the address of the VMethodCall *
											//     right after the CALL instruction on the trampoline.
		mov ebx, eax;						//  3. Store the VMethodCall ** in ebx.
		mov ebx, [ebx];						//  4. Dereference the VMethodCall **.
		sub eax, 5;							//  5. Rewind the pointer to the start of the VMethodTrampoline structure.
		mov [esp+8+0], eax;					//  6. Store the VMethodTrampoline * on the reserved spot so that we can access it from
											//     C++ through the second argument.
		mov eax, [ebx+VMethodCall::m_returnAddress];	//  6. Get the return address of the caller.
		mov [esp+8+4], eax;					//  7. Replace the trampoline return-address with the return address of the caller.
		pop ebx;
		pop eax;

		pushad;								//  8. Save all registers and conveniently place them so that they're available
											//     from C++ through the first argument.

		sub esp, 4;							//  9. Padding/fake return address so that ebp+8 refers to the first argument.
		push ebp;							// 10. Standard prolog.
		mov ebp, esp;
		sub esp, __LOCAL_SIZE;
	}

	call = (VMethodCall *) trampoline->data;
	call->GetMethod()->OnLeaveWrapper(&cpuCtx, trampoline, call);

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
VMethod::OnLeaveWrapper(CpuContext *cpuCtx, VMethodTrampoline *trampoline, VMethodCall *call)
{
	// Got this now
	call->SetCpuContextLeave(cpuCtx);

	// Do some logging
	OnLeave(call);

	delete trampoline;
	delete call;
}

void
VMethod::OnEnter(VMethodCall *call)
{
	VMethodCallHandler handler = call->GetMethod()->GetSpec()->GetEnterHandler();

	if (handler == NULL || !handler(call))
	{
		message_logger_log_message("VMethod::OnEnter", call->GetBacktraceAddress(),
			MESSAGE_CTX_INFO, "Entering %s @ 0x%08x, ecx = 0x%08x",
			call->ToString().c_str(), GetOffset(),
			call->GetCpuContextEnter()->ecx);
	}
}

void
VMethod::OnLeave(VMethodCall *call)
{
	VMethodCallHandler handler = call->GetMethod()->GetSpec()->GetLeaveHandler();

	if (handler == NULL || !handler(call))
	{
		message_logger_log_message("VMethod::OnLeave", call->GetBacktraceAddress(),
			MESSAGE_CTX_INFO, "Leaving %s @ 0x%08x, eax = 0x%08x",
			call->ToString().c_str(), GetOffset(),
			call->GetCpuContextLeave()->eax);
	}
}

OString
VMethodCall::ToString() const
{
	VMethodSpec *spec = m_method->GetSpec();

	OStringStream ss;

	ss << m_method->GetVTable()->GetName();
	ss << "::";
	ss << spec->GetName();

	int argsSize = spec->GetArgsSize();
	if (argsSize != VMETHOD_ARGS_SIZE_UNKNOWN && argsSize % sizeof(DWORD) == 0)
	{
		ss << "(";

		DWORD *args = (DWORD *) m_argumentsData.data();

		for (int i = 0; i < argsSize / sizeof(DWORD); i++)
		{
			if (i)
				ss << ", ";

			// FIXME: optimize this
			if (args[i] > 0xFFFF && !IsBadReadPtr((void *) args[i], 1))
				ss << hex << "0x";
			else
				ss << dec;

			ss << args[i];
		}

		ss << ")";
	}

	return ss.str();
}

};