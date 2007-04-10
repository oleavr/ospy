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

#pragma once

#include <winsock2.h>
#include "BinaryLogger.h"

#define MAX_SOFTWALL_RULES   128

typedef struct {
    /* mask of conditions */
    DWORD Conditions;

    /* condition values */
    char ProcessName[32];
    char FunctionName[16];
    DWORD ReturnAddress;
    in_addr LocalAddress;
    int LocalPort;
    in_addr RemoteAddress;
    int RemotePort;

    /* return value and lasterror to set if all conditions match */
    int Retval;
    DWORD LastError;
} SoftwallRule;

typedef struct {
    WCHAR LogPath[MAX_PATH];
    volatile LONG LogIndex;
    volatile LONG LogSize;

    DWORD NumSoftwallRules;
    SoftwallRule rules[MAX_SOFTWALL_RULES];
} Capture;

class Agent : public BaseObject
{
public:
    Agent();

    void Initialize();
    void UnInitialize();

    LONG GetNextLogIndex();
    LONG AddBytesLogged(LONG n);

protected:
    HANDLE m_map;
    Capture *m_capture;
};
