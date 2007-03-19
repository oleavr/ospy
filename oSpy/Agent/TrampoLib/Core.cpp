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
#include "Core.h"
#include "..\logging.h" // FIXME: YUCK

namespace TrampoLib {

const PrologSignatureSpec Function::prologSignatureSpecs[] = {
	{
		{
			NULL,
            0,
			"8B FF"					// mov edi, edi
			"55"					// push ebp
			"8B EC",				// mov ebp, esp
		},

		5,
	},
	{
		{
			NULL,
            0,
			"6A ??"					// push ??h
			"68 ?? ?? ?? ??"		// push offset dword_????????
			"E8 ?? ?? ?? ??",		// call __SEH_prolog
		},

		7,
	},
	{
		{
			NULL,
            0,
			"68 ?? ?? ?? ??"		// push ???h
			"68 ?? ?? ?? ??"		// push offset dword_????????
			"E8 ?? ?? ?? ??",		// call __SEH_prolog
		},

		5,
	},
	{
		{
			NULL,
            0,
			"FF 25 ?? ?? ?? ??"		// jmp ds:__imp__*
		},

		6,
	},
	{
		{
			NULL,
            0,
			"33 C0"		// xor eax, eax
			"50"		// push eax
			"50"		// push eax
			"6A ??"		// push ??
		},

		6,
	},
};

OVector<Signature>::Type Function::prologSignatures;

namespace Type {

OString
UInt32::ToString(const void *start) const
{
    OOStringStream ss;

    const DWORD *dwPtr = reinterpret_cast<const DWORD *>(start);
    ss << *dwPtr;

    return ss.str();
}

OString
AsciiStringPtr::ToString(const void *start) const
{
    OOStringStream ss;

    // FIXME: use C++ style cast here
    const char **strPtr = (const char **) start;
    if (*strPtr != NULL)
    {
        ss << "\"" << *strPtr << "\"";
    }
    else
    {
        ss << "NULL";
    }

    return ss.str();
}

OString
UnicodeStringPtr::ToString(const void *start) const
{
    OOStringStream ss;

    // FIXME: use C++ style cast here
    const WCHAR **strPtr = (const WCHAR **) start;
    if (*strPtr != NULL)
    {
        int bufSize = static_cast<int>(wcslen(*strPtr)) + 1;
        char *buf = new char[bufSize];

        WideCharToMultiByte(CP_ACP, 0, *strPtr, -1, buf, bufSize, NULL, NULL);

        ss << "\"" << buf << "\"";

        delete buf;
    }
    else
    {
        ss << "NULL";
    }

    return ss.str();
}

}

FunctionArgumentList::FunctionArgumentList(unsigned int count, ...)
{
    va_list args;
    va_start(args, count);

    Initialize(count, args);

    va_end(args);
}

FunctionArgumentList::FunctionArgumentList(unsigned int count, va_list args)
{
    Initialize(count, args);
}

FunctionArgumentList::~FunctionArgumentList()
{
    for (unsigned int i = 0; i < m_args.size(); i++)
    {
		delete m_args[i];
	}
}

void
FunctionArgumentList::Initialize(unsigned int count, va_list args)
{
    m_size = 0;

    for (unsigned int i = 0; i < count; i++)
    {
		FunctionArgument::Base *arg = va_arg(args, FunctionArgument::Base *);

        m_args.push_back(arg);

        m_size += arg->GetSize();
    }
}

void
FunctionSpec::SetArgumentList(FunctionArgumentList *argList)
{
    m_argsSize = argList->GetSize();
    m_argList = argList;
}

void
FunctionSpec::SetArgumentList(unsigned int count, ...)
{
    va_list args;
    va_start(args, count);

    m_argList = new FunctionArgumentList(count, args);
    m_argsSize = m_argList->GetSize();

    va_end(args);
}

void
Function::Initialize()
{
	for (int i = 0; i < sizeof(prologSignatureSpecs) / sizeof(PrologSignatureSpec); i++)
	{
        prologSignatures.push_back(Signature(&prologSignatureSpecs[i].sig));
    }
}

FunctionTrampoline *
Function::CreateTrampoline(unsigned int bytesToCopy)
{
    FunctionTrampoline *trampoline = reinterpret_cast<FunctionTrampoline *>(new unsigned char[sizeof(FunctionTrampoline) + bytesToCopy + sizeof(FunctionRedirectStub)]);

	trampoline->CALL_opcode = 0xE8;
	trampoline->CALL_offset = (DWORD) OnEnterProxy - (DWORD) &(trampoline->data);
	trampoline->data = this;

    if (bytesToCopy > 0)
    {
        memcpy(reinterpret_cast<unsigned char *>(trampoline) + sizeof(FunctionTrampoline), reinterpret_cast<const void *>(m_offset), bytesToCopy);
    }

    FunctionRedirectStub *redirStub = reinterpret_cast<FunctionRedirectStub *>(reinterpret_cast<unsigned char *>(trampoline) + sizeof(FunctionTrampoline) + bytesToCopy);
    redirStub->JMP_opcode = 0xE9;
    redirStub->JMP_offset = (m_offset + bytesToCopy) - (reinterpret_cast<DWORD>(reinterpret_cast<unsigned char *>(redirStub) + sizeof(FunctionRedirectStub)));

	return trampoline;
}

void
Function::Hook()
{
    const PrologSignatureSpec *spec = NULL;

    for (unsigned int i = 0; i < prologSignatures.size(); i++)
    {
        const Signature *sig = &prologSignatures[i];

        OVector<void *>::Type matches = SignatureMatcher::Instance()->FindInRange(sig, reinterpret_cast<void *>(m_offset), sig->GetLength());
        if (matches.size() == 1)
        {
            spec = &prologSignatureSpecs[i];
            break;
        }
    }

    if (spec == NULL)
        throw runtime_error("none of the supported signatures matched");

    FunctionTrampoline *trampoline = CreateTrampoline(spec->numBytesToCopy);

    DWORD oldProtect;
    VirtualProtect(reinterpret_cast<LPVOID>(m_offset), 5, PAGE_EXECUTE_WRITECOPY, &oldProtect);
    // TODO: check that VirtualProtect succeeded

    FunctionRedirectStub *redirStub = reinterpret_cast<FunctionRedirectStub *>(m_offset);
    redirStub->JMP_opcode = 0xE9;
    redirStub->JMP_offset = reinterpret_cast<DWORD>(trampoline) - (reinterpret_cast<DWORD>(reinterpret_cast<unsigned char *>(redirStub) + sizeof(FunctionRedirectStub)));

    FlushInstructionCache(GetCurrentProcess(), NULL, 0);
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
		*proxyRet = reinterpret_cast<unsigned char *>(trampoline) + sizeof(FunctionTrampoline);
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
    call->SetState(FUNCTION_CALL_STATE_LEAVING);

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
	FunctionCallHandler handler = call->GetFunction()->GetSpec()->GetHandler();

	if (handler == NULL || !handler(call))
	{
		message_logger_log_message("Function::OnEnter", call->GetBacktraceAddress(),
			MESSAGE_CTX_INFO, "Entering %s @ 0x%08x, ecx = 0x%08x",
			call->ToString().c_str(), GetOffset(),
			call->GetCpuContextEnter()->ecx);
	}
}

void
Function::OnLeave(FunctionCall *call)
{
	FunctionCallHandler handler = call->GetFunction()->GetSpec()->GetHandler();

	if (handler == NULL || !handler(call))
	{
		message_logger_log_message("Function::OnLeave", call->GetBacktraceAddress(),
			MESSAGE_CTX_INFO, "Leaving %s @ 0x%08x, eax = 0x%08x",
			call->ToString().c_str(), GetOffset(),
			call->GetCpuContextLeave()->eax);
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

    FunctionArgumentList *args = spec->GetArgumentList();
    if (args != NULL)
    {
        ss << "(";

        const unsigned char *argsData = reinterpret_cast<const unsigned char *>(m_argumentsData.data());

        for (unsigned int i = 0; i < args->GetCount(); i++)
        {
            FunctionArgument::Base *arg = (*args)[i];

            if (i)
			    ss << ", ";

            ss << arg->ToString(argsData);

            argsData += arg->GetSize();
        }

        ss << ")";
    }
    else
    {
	    int argsSize = spec->GetArgsSize();
	    if (argsSize != FUNCTION_ARGS_SIZE_UNKNOWN && argsSize % sizeof(DWORD) == 0)
	    {
		    ss << "(";

		    DWORD *args = (DWORD *) m_argumentsData.data();

		    for (unsigned int i = 0; i < argsSize / sizeof(DWORD); i++)
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
    }

	return ss.str();
}

} // namespace TrampoLib