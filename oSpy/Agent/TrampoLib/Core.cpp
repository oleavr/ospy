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
// FROM, OUT OF OR IN CONNECTION WI
//

#include "stdafx.h"
#include "Core.h"

namespace TrampoLib {

const PrologSignature Function::prologSignatures[] = {
	{
		{
			NULL,

			"8B FF"					// mov edi, edi
			"55"					// push ebp
			"8B EC",				// mov ebp, esp
		},

		5,
		5,
	},
	{
		{
			NULL,

			"6A ??"					// push ??h
			"68 ?? ?? ?? ??"		// push offset dword_????????
			"E8 ?? ?? ?? ??",		// call __SEH_prolog
		},

		12,
		7,
	},
	{
		{
			NULL,

			"68 ?? ?? ?? ??"		// push ???h
			"68 ?? ?? ?? ??"		// push offset dword_????????
			"E8 ?? ?? ?? ??",		// call __SEH_prolog
		},

		15,
		5,
	},
	{
		{
			NULL,

			"FF 25 ?? ?? ?? ??"		// jmp ds:__imp__*
		},

		6,
		6,
	},
	{
		{
			NULL,

			"33 C0"		// xor eax, eax
			"50"		// push eax
			"50"		// push eax
			"6A ??"		// push ??
		},

		6,
		6,
	},
};

FunctionTrampoline *
Function::CreateTrampoline()
{
	FunctionTrampoline *trampoline = new FunctionTrampoline;

	trampoline->CALL_opcode = 0xE8;
	trampoline->CALL_offset = (DWORD) OnEnterProxy - (DWORD) &(trampoline->data);
	trampoline->data = this;

	return trampoline;
}

void
Function::Hook()
{
}

__declspec(naked) void
Function::OnEnterProxy(CpuContext cpuCtx, unsigned int unwindSize, FunctionTrampoline *trampoline, void **proxyRet, void **finalRet)
{
	DWORD lastError;
	Function *function;
	FunctionTrampoline *nextTrampoline;

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

	function = static_cast<Function *>(trampoline->data);
	nextTrampoline = function->OnEnterWrapper(&cpuCtx, &unwindSize, trampoline, finalRet, &lastError);
	if (nextTrampoline != NULL)
	{
		*proxyRet = reinterpret_cast<void *>(function->GetOffset());
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

FunctionTrampoline *
Function::OnEnterWrapper(CpuContext *cpuCtx, unsigned int *unwindSize, FunctionTrampoline *trampoline, void *btAddr, DWORD *lastError)
{
	// Keep track of the function call
	FunctionCall *call = new FunctionCall(this, btAddr, cpuCtx);
	call->SetCpuContextLive(cpuCtx);
	call->SetLastErrorLive(lastError);

	OnEnter(call);

	bool carryOn = call->GetShouldCarryOn();

	FunctionSpec *spec = call->GetFunction()->GetSpec();
	CallingConvention conv = spec->GetCallingConvention();
	if (conv == CALLING_CONV_UNKNOWN ||
		(conv != CALLING_CONV_CDECL && spec->GetArgsSize() == FUNCTION_ARGS_SIZE_UNKNOWN))
	{
		// TODO: log a warning here
		carryOn = true;
	}

	if (carryOn)
	{
		// Set up a trampoline used to trap the return
		FunctionTrampoline *retTrampoline = new FunctionTrampoline;

		retTrampoline->CALL_opcode = 0xE8;
		retTrampoline->CALL_offset = (DWORD) Function::OnLeaveProxy - (DWORD) &(retTrampoline->data);
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
Function::OnLeaveProxy(CpuContext cpuCtx, FunctionTrampoline *trampoline)
{
	FunctionCall *call;
	DWORD lastError;

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
		mov eax, [ebx+FunctionCall::m_returnAddress];	//  6. Get the return address of the caller.
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

    lastError = GetLastError();

	call = static_cast<FunctionCall *>(trampoline->data);
	call->GetFunction()->OnLeaveWrapper(&cpuCtx, trampoline, call, &lastError);

    SetLastError(lastError);

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
Function::OnLeaveWrapper(CpuContext *cpuCtx, FunctionTrampoline *trampoline, FunctionCall *call, DWORD *lastError)
{
	call->SetCpuContextLive(cpuCtx);
	call->SetLastErrorLive(lastError);

    // Got this now
	call->SetCpuContextLeave(cpuCtx);

	// Do some logging
	OnLeave(call);

	delete trampoline;
	delete call;
}

void
Function::OnEnter(FunctionCall *call)
{
	FunctionCallHandler handler = call->GetFunction()->GetSpec()->GetEnterHandler();

	if (handler == NULL || !handler(call))
	{
#if 0
		message_logger_log_message("VMethod::OnEnter", call->GetBacktraceAddress(),
			MESSAGE_CTX_INFO, "Entering %s @ 0x%08x, ecx = 0x%08x",
			call->ToString().c_str(), GetOffset(),
			call->GetCpuContextEnter()->ecx);
#endif
	}
}

void
Function::OnLeave(FunctionCall *call)
{
	FunctionCallHandler handler = call->GetFunction()->GetSpec()->GetLeaveHandler();

	if (handler == NULL || !handler(call))
	{
#if 0
		message_logger_log_message("VMethod::OnLeave", call->GetBacktraceAddress(),
			MESSAGE_CTX_INFO, "Leaving %s @ 0x%08x, eax = 0x%08x",
			call->ToString().c_str(), GetOffset(),
			call->GetCpuContextLeave()->eax);
#endif
	}
}

OString
FunctionCall::ToString() const
{
	FunctionSpec *spec = m_function->GetSpec();

	OOStringStream ss;

    const OString &parentName = m_function->GetParentName();
    if (parentName.length() > 0)
    {
        ss << parentName << "::";
    }

	ss << spec->GetName();

	int argsSize = spec->GetArgsSize();
	if (argsSize != FUNCTION_ARGS_SIZE_UNKNOWN && argsSize % sizeof(DWORD) == 0)
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

} // namespace TrampoLib