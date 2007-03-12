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

VTableHooker *
VTableHooker::Self()
{
	static VTableHooker *hooker = new VTableHooker();
	return hooker;
}

void
VTableHooker::HookVTable(VTableSpec &vtable)
{
	DWORD oldProtect;
	VirtualProtect((LPVOID) vtable.GetStartOffset(), vtable.GetMethodCount() * sizeof(LPVOID),
		PAGE_READWRITE, &oldProtect);

	DWORD *methods = (DWORD *) vtable.GetStartOffset();

	for (int i = 0; i < vtable.GetMethodCount(); i++)
	{
		unsigned char *trampoline = new unsigned char[5 + sizeof(VMethodSpec *)];
		int offset = 0;

		trampoline[offset++] = 0xe8;
		*((DWORD *) (trampoline + offset)) = (DWORD) VTableHooker::VTableProxyFunc - (DWORD) (trampoline + offset + 4);
		offset += sizeof(DWORD);

		*((VMethodSpec **) (trampoline + offset)) = &vtable[i];

		methods[i] = (DWORD) trampoline;
	}
}

__declspec(naked) void
VTableHooker::VTableProxyFunc(CpuContext cpuCtx, VMethodSpec *methodSpec)
{
	__asm {
		sub esp, 4;
		push eax;
		push ebx;
		mov eax, [esp+12];
		mov eax, [eax]; // dereference VMethodSpec **
		mov ebx, eax; // VMethodSpec *
		mov eax, [eax+VMethodSpec::m_offset];
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

	message_logger_log_message("VTableProxyFunc", 0, MESSAGE_CTX_INFO,
		"Index=%d, Offset=0x%08x, Name=%s",
		methodSpec->GetIndex(), methodSpec->GetOffset(), methodSpec->GetName().c_str());

	__asm {
		mov esp, ebp;
		pop ebp;
		add esp, 4;

		popad;

		add esp, 4;
		ret;
	}
}

VTableSpec::VTableSpec(const OString &name, DWORD startOffset, int methodCount)
	: m_name(name), m_startOffset(startOffset), m_methods(methodCount)
{
	DWORD *funcs = (DWORD *) startOffset;

	for (int i = 0; i < methodCount; i++)
	{
		m_methods[i].Initialize(this, i, funcs[i]);
	}
}

void
VMethodSpec::Initialize(VTableSpec *vtable, int index, DWORD offset)
{
	m_vtable = vtable;
	m_index = index;
	m_offset = offset;

	m_name = "Method_";
	m_name += index;
}
