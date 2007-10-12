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

#include "stdafx.h"
#include "VTable.h"

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

void
VMethodSpec::Initialize(VTableSpec *vtable, int index)
{
	m_vtable = vtable;
	m_index = index;

	OOStringStream ss;
	ss << "method_" << index;
	m_name = ss.str();
}

void
VMethodSpec::StealFrom(FunctionSpec *funcSpec)
{
    if (funcSpec->GetName().size() > 0)
    {
        m_name = funcSpec->GetName();
    }

    if (funcSpec->GetCallingConvention() != CALLING_CONV_UNKNOWN)
    {
        m_callingConvention = funcSpec->GetCallingConvention();
    }

    if (funcSpec->GetArgsSize() != FUNCTION_ARGS_SIZE_UNKNOWN)
    {
        m_argsSize = funcSpec->GetArgsSize();
    }

    if (funcSpec->GetHandler() != NULL)
    {
        m_handler = funcSpec->GetHandler();
    }

	if (funcSpec->GetHandlerUserData() != NULL)
	{
		m_handlerUserData = funcSpec->GetHandlerUserData();
	}

    if (funcSpec->GetArguments() != NULL)
    {
        m_argList = funcSpec->GetArguments();
        funcSpec->SetArguments(static_cast<ArgumentListSpec *>(NULL));
    }

    const BaseMarshaller *marshaller = funcSpec->GetReturnValueMarshaller();
    if (marshaller != NULL)
    {
        m_retValMarshaller = marshaller->Clone();
    }

    m_logNestedCalls = funcSpec->GetLogNestedCalls();
}

VTableSpec::VTableSpec(const OString &name, int methodCount)
	: m_name(name), m_methods(methodCount)
{
	for (int i = 0; i < methodCount; i++)
	{
		m_methods[i].Initialize(this, i);
	}
}

VTable::VTable(VTableSpec *spec, const OString &name, DWORD startOffset)
	: m_spec(spec), m_name(name), m_startOffset(startOffset), m_methods(spec->GetMethodCount())
{
	DWORD *funcs = reinterpret_cast<DWORD *>(startOffset);

	for (unsigned int i = 0; i < spec->GetMethodCount(); i++)
	{
		m_methods[i].Initialize(this, &(*spec)[i], funcs[i]);
	}
}

void
VTable::Hook()
{
	VTableSpec *spec = GetSpec();

    DWORD vtSize = spec->GetMethodCount() * sizeof(LPVOID);

    DWORD oldProtect;
	VirtualProtect(reinterpret_cast<LPVOID>(m_startOffset), vtSize, PAGE_READWRITE, &oldProtect);

	DWORD *methods = reinterpret_cast<DWORD *>(m_startOffset);

	for (unsigned int i = 0; i < spec->GetMethodCount(); i++)
	{
		methods[i] = reinterpret_cast<DWORD>(m_methods[i].CreateTrampoline());
	}

    FlushInstructionCache(GetCurrentProcess(), NULL, 0);

	VirtualProtect(reinterpret_cast<LPVOID>(m_startOffset), vtSize, oldProtect, &oldProtect);
}

void
VTable::UnHook()
{
    VTableSpec *spec = GetSpec();

    DWORD vtSize = spec->GetMethodCount() * sizeof(LPVOID);

    DWORD oldProtect;
	VirtualProtect(reinterpret_cast<LPVOID>(m_startOffset), vtSize, PAGE_READWRITE, &oldProtect);

    DWORD *methods = reinterpret_cast<DWORD *>(m_startOffset);

	for (unsigned int i = 0; i < spec->GetMethodCount(); i++)
	{
		void *trampoline = reinterpret_cast<void *>(methods[i]);

        methods[i] = m_methods[i].GetOffset();

        delete[] trampoline;
	}

    FlushInstructionCache(GetCurrentProcess(), NULL, 0);

	VirtualProtect(reinterpret_cast<LPVOID>(m_startOffset), vtSize, oldProtect, &oldProtect);
}

} // namespace InterceptPP
