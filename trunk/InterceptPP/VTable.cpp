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
#include "VTable.h"

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

void
VMethodSpec::Initialize(VTableSpec *vtable, int index)
{
	m_vtable = vtable;
	m_index = index;

	OOStringStream ss;
	ss << "Method_" << index;
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

    if (funcSpec->GetArguments() != NULL)
    {
        m_argList = funcSpec->GetArguments();
        funcSpec->SetArguments(static_cast<ArgumentListSpec *>(NULL));
    }
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

	DWORD oldProtect;
	VirtualProtect(reinterpret_cast<LPVOID>(m_startOffset), spec->GetMethodCount() * sizeof(LPVOID),
		PAGE_READWRITE, &oldProtect);

	DWORD *methods = reinterpret_cast<DWORD *>(m_startOffset);

	for (unsigned int i = 0; i < spec->GetMethodCount(); i++)
	{
		methods[i] = reinterpret_cast<DWORD>(m_methods[i].CreateTrampoline());
	}

    FlushInstructionCache(GetCurrentProcess(), NULL, 0);
}

} // namespace InterceptPP