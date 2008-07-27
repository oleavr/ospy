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

#include "AgentPlugin.h"
#include "BinaryLogger.h"

// FIXME: move this into a separate plugin once the planned plugin architecture has been introduced
//#define RESEARCH_MODE

namespace oSpy {

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
    volatile LONG ActiveAgentCount;

    WCHAR LogPath[MAX_PATH];
    volatile ULONG LogIndexUserspace;
    volatile ULONG LogCount;
    volatile ULONG LogSize;

    DWORD NumSoftwallRules;
    SoftwallRule Rules[MAX_SOFTWALL_RULES];
} Capture;

class Agent : public BaseObject
{
public:
    Agent ();

    void Initialize ();
    void UnInitialize ();

    ULONG GetNextLogIndex ();
    ULONG AddBytesLogged (ULONG n);

private:
    void LoadPlugins ();
    void UnloadPlugins ();

    OWString GetBinPath () const;

protected:
    HANDLE m_map;
    Capture * m_capture;
    oSpy::BinaryLogger * m_binaryLogger;
    OICString m_processName;

    typedef OMap<const AgentPluginDesc *, AgentPlugin *>::Type PluginMap;
    typedef OList<HMODULE>::Type PluginModuleList;
    PluginMap m_plugins;
    PluginModuleList m_pluginModules;

    FunctionCallHandler<Agent> m_socketConnectHandler;
    void OnSocketConnect (FunctionCall * call, bool & shouldLog);

#ifdef RESEARCH_MODE
    static void OnWaitForSingleObject(FunctionCall *call, void *userData, bool &shouldLog);
    static void OnWaitForMultipleObjects(FunctionCall *call, void *userData, bool &shouldLog);

    static void PacketSchedulerRunAfterGetSendQueue();
#endif

    bool HaveMatchingSoftwallRule(const OString &functionName, void *returnAddress, const sockaddr_in *localAddress, const sockaddr_in *peerAddress, DWORD &retval, DWORD &lastError);
};

} // namespace oSpy