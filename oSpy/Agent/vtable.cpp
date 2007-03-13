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
		unsigned char *trampoline = new unsigned char[5 + sizeof(VMethodSpec *)];
		int offset = 0;

		trampoline[offset++] = 0xe8;
		*((DWORD *) (trampoline + offset)) = (DWORD) VTableProxyFunc - (DWORD) (trampoline + offset + 4);
		offset += sizeof(DWORD);

		*((VMethod **) (trampoline + offset)) = &m_methods[i];

		methods[i] = (DWORD) trampoline;
	}
}

__declspec(naked) void
VTable::VTableProxyFunc(CpuContext cpuCtx, VMethod *method)
{
	VMethodSpec *spec;

	__asm {
		sub esp, 4;
		push eax;
		push ebx;
		mov eax, [esp+12];
		mov eax, [eax]; // dereference VMethod **
		mov ebx, eax; // VMethod *
		mov eax, [eax+VMethod::m_offset];
		mov [esp+12], eax;
		mov [esp+8], ebx;
		pop ebx;
		pop eax;

		pushad;

		sub esp, 4; // padding
		push ebp;
		mov ebp, esp;
		sub esp, __LOCAL_SIZE;
	}

	spec = method->GetSpec();

	message_logger_log_message("VTableProxyFunc", 0, MESSAGE_CTX_INFO,
		"Index=%d, Offset=0x%08x, Name=%s",
		spec->GetIndex(), method->GetOffset(), spec->GetName().c_str());

	__asm {
		mov esp, ebp;
		pop ebp;
		add esp, 4;

		popad;

		add esp, 4;
		ret;
	}
}
