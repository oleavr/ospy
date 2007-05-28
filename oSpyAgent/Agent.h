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

// FIXME: move this into a separate plugin once the planned plugin architecture has been introduced
//#define RESEARCH_MODE

#define MAX_SOFTWALL_RULES   128

#define SOFTWALL_CONDITION_PROCESS_NAME    1
#define SOFTWALL_CONDITION_FUNCTION_NAME   2
#define SOFTWALL_CONDITION_RETURN_ADDRESS  4
#define SOFTWALL_CONDITION_LOCAL_ADDRESS   8
#define SOFTWALL_CONDITION_LOCAL_PORT     16
#define SOFTWALL_CONDITION_PEER_ADDRESS   32
#define SOFTWALL_CONDITION_PEER_PORT      64

typedef struct {
    /* mask of conditions */
    DWORD Conditions;

    /* condition values */
    char ProcessName[32];
    char FunctionName[16];
    void *ReturnAddress;
    in_addr LocalAddress;
    int LocalPort;
    in_addr PeerAddress;
    int PeerPort;

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
    OICString m_processName;

	static void OnSocketConnectWrapper(FunctionCall *call, void *userData, bool &shouldLog);
	void OnSocketConnect(FunctionCall *call);

#ifdef RESEARCH_MODE
	static void OnWaitForSingleObject(FunctionCall *call, void *userData, bool &shouldLog);
	static void OnWaitForMultipleObjects(FunctionCall *call, void *userData, bool &shouldLog);
#endif

    bool HaveMatchingSoftwallRule(const OString &functionName, void *returnAddress, const sockaddr_in *localAddress, const sockaddr_in *peerAddress, DWORD &retval, DWORD &lastError);
};
