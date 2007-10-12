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
