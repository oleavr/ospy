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
#include "hooking.h"
#include "logging.h"

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
