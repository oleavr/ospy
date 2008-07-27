//
// Copyright (c) 2008 Ole André Vadla Ravnås <oleavr@gmail.com>
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

#include <InterceptPP/InterceptPP.h>
#include <InterceptPP/HookManager.h>
#include <InterceptPP/Core.h>

namespace oSpy {

typedef struct _AgentPluginDesc AgentPluginDesc;
class AgentPlugin;

typedef const oSpy::AgentPluginDesc * (__cdecl * AgentPluginGetDescFunc) ();
typedef AgentPlugin * (WINAPI * AgentPluginCreateFunc) ();
typedef void (WINAPI * AgentPluginDestroyFunc) (AgentPlugin * plugin);

struct _AgentPluginDesc
{
    DWORD ApiVersion;
    WCHAR Name[32];
    WCHAR Description[128];

    AgentPluginCreateFunc CreateFunc;
    AgentPluginDestroyFunc DestroyFunc;
};

class AgentPlugin
{
public:
    virtual ~AgentPlugin () {}

    void Initialize (const char * processName, InterceptPP::HookManager * hookManager)
    {
        m_processName = processName;
        m_hookManager = hookManager;
    }

    virtual bool Open () = 0;
    virtual void Close () = 0;

protected:
    std::string m_processName;
    InterceptPP::HookManager * m_hookManager;
};

#define OSPY_AGENT_PLUGIN_DEFINE(API_VERSION, NAME, DESCRIPTION, PREFIX) \
    static oSpy::AgentPlugin * WINAPI\
    PREFIX##CreateFunc ()\
    {\
        return new PREFIX##Plugin ();\
    }\
\
    static void WINAPI\
    PREFIX##DestroyFunc (oSpy::AgentPlugin * plugin)\
    {\
        delete plugin;\
    }\
\
    static const oSpy::AgentPluginDesc PREFIX##Desc = { API_VERSION, NAME, DESCRIPTION, PREFIX##CreateFunc, PREFIX##DestroyFunc };\
\
    extern "C" __declspec(dllexport) const oSpy::AgentPluginDesc * __cdecl\
    oSpyAgentPluginGetDesc ()\
    {\
        return &PREFIX##Desc;\
    }

} // namespace oSpy