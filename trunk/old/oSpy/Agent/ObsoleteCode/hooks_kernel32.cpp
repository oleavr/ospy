//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

#include "stdafx.h"
#include "hooking.h"
#include "logging_old.h"

#define DEVICEIOCONTROL_ARGS_SIZE (8 * 4)

static BOOL __cdecl
DeviceIoControl_called(BOOL carry_on,
                       DWORD ret_addr,
                       HANDLE hDevice,
                       DWORD dwIoControlCode,
                       LPVOID lpInBuffer,
                       DWORD nInBufferSize,
                       LPVOID lpOutBuffer,
                       DWORD nOutBufferSize,
                       LPDWORD lpBytesReturned,
                       LPOVERLAPPED lpOverlapped)
{
	void *bt_address = (char *) &carry_on + 8 + DEVICEIOCONTROL_ARGS_SIZE;

	message_logger_log("DeviceIoControl", bt_address, (DWORD) hDevice, MESSAGE_TYPE_PACKET, MESSAGE_CTX_INFO,
					   PACKET_DIRECTION_OUTGOING, NULL, NULL, (const char *) lpInBuffer, nInBufferSize,
					   "hDevice=0x%08x, dwIoControlCode=0x%08x, nInBufferSize=%d, nOutBufferSize=%d",
					   hDevice, dwIoControlCode, nInBufferSize, nOutBufferSize);

    return TRUE;
}

static BOOL __stdcall
DeviceIoControl_done(BOOL retval,
                     HANDLE hDevice,
                     DWORD dwIoControlCode,
                     LPVOID lpInBuffer,
                     DWORD nInBufferSize,
                     LPVOID lpOutBuffer,
                     DWORD nOutBufferSize,
                     LPDWORD lpBytesReturned,
                     LPOVERLAPPED lpOverlapped)
{
    DWORD err = GetLastError();
    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));

    SetLastError(err);
    return retval;
}

HOOK_GLUE_SPECIAL(DeviceIoControl, DEVICEIOCONTROL_ARGS_SIZE)

void
hook_kernel32()
{
    HMODULE h = LoadLibrary("kernel32.dll");
    if (h == NULL)
    {
	    MessageBox(0, "Failed to load 'kernel32.dll'.",
                   "oSpy", MB_ICONERROR | MB_OK);
		return;
    }
	
	HOOK_FUNCTION_SPECIAL(h, DeviceIoControl);
}
