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
VTableHooker::HookVTableAt(void *startOffset, int numFuncs)
{
	DWORD *vtable = (DWORD *) startOffset;
	DWORD oldProtect;

	VirtualProtect(startOffset, numFuncs * sizeof(DWORD), PAGE_EXECUTE_WRITECOPY, &oldProtect);

	for (int i = 0; i < numFuncs; i++)
	{
		unsigned char *trampoline = new unsigned char[5 + sizeof(VFuncContext)];
		int offset = 0;

		trampoline[offset++] = 0xe8;
		*((DWORD *) (trampoline + offset)) = (DWORD) VTableHooker::VTableProxyFunc - (DWORD) (trampoline + offset + 4);
		offset += sizeof(DWORD);

		VFuncContext *ctx = (VFuncContext *) (trampoline + offset);
		ctx->index = i;
		ctx->functionStart = vtable[i];

		vtable[i] = (DWORD) trampoline;
	}
}

__declspec(naked) void
VTableHooker::VTableProxyFunc(CpuContext cpuCtx, VFuncContext *funcCtx)
{
	__asm {
		sub esp, 4;
		push eax;
		push ebx;
		mov eax, [esp+12];
		mov ebx, eax;
		mov eax, [eax+4];
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

	message_logger_log_message("VTableProxyFunc", 0, MESSAGE_CTX_INFO, "index=%d, functionStart=0x%08x", funcCtx->index, funcCtx->functionStart);

	__asm {
		mov esp, ebp;
		pop ebp;
		add esp, 4;

		popad;

		add esp, 4;
		ret;
	}
}
