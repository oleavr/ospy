//
// Copyright (c) 2006-2008 Ole André Vadla Ravnås <oleavr@gmail.com>
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
#include "Agent.h"

#include <shlwapi.h>

#define OSPY_AGENT_MODULE_NAME              L"oSpyAgent.dll"
#define OSPY_AGENT_START_REQUEST_EVENT_NAME L"oSpyAgentStartRequest"
#define OSPY_AGENT_STOP_REQUEST_EVENT_NAME  L"oSpyAgentStopRequest"
#define OSPY_AGENT_STOP_RESPONSE_EVENT_NAME L"oSpyAgentStopResponse"

#define OSPY_CAPTURE_FILE_NAME      L"Global\\oSpyCapture"

#ifdef _MANAGED
#pragma managed(push, off)
#endif

namespace oSpy {

class WinError : public Error
{
public:
    WinError(const OString & funcName)
        : Error("")
    {
        OOStringStream ss;
        ss << funcName << " failed: 0x" << hex << setw(8) << setfill('0') << GetLastError();
        m_what = ss.str();
    }
};

Agent::Agent()
    : m_map(INVALID_HANDLE_VALUE),
      m_capture (NULL),
      m_binaryLogger (NULL)
{
    m_socketConnectHandler.Initialize (this, &Agent::OnSocketConnect);
}

void
Agent::Initialize()
{
    InterceptPP::Initialize();

    m_map = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, OSPY_CAPTURE_FILE_NAME);
    if (m_map == NULL)
        throw WinError("OpenFileMapping");

    m_capture = static_cast<Capture *>(MapViewOfFile(m_map, FILE_MAP_WRITE, 0, 0, sizeof(Capture)));
    if (m_capture == NULL)
        throw WinError("MapViewOfFile");

    // Let the UI know about us
    InterlockedIncrement (&m_capture->ActiveAgentCount);

    // Create the logger and tell Intercept++ to use it
    {
        OOWStringStream ss;
        ss << m_capture->LogPath << "\\" << GetProcessId(GetCurrentProcess()) << ".log";
        m_binaryLogger = new BinaryLogger (this, ss.str ());

        InterceptPP::SetLogger (m_binaryLogger);
    }

    m_processName = Util::Instance()->GetProcessName().c_str();

    // Load hook definitions from XML
    HookManager *mgr = HookManager::Instance();
    try
    {
        OOWStringStream ss;
        ss << m_capture->LogPath << "\\" << "config.xml";
        mgr->LoadDefinitions(ss.str());
    }
    catch (Error &e)
    {
        GetLogger()->LogError("LoadDefinitions failed: %s", e.what());
        return;
    }
    catch (...)
    {
        GetLogger()->LogError("LoadDefinitions failed: unknown error");
        return;
    }

    // Trap calls to socket functions for softwalling
    FunctionSpec * funcSpec = mgr->GetFunctionSpecById ("connect");
    if (funcSpec != NULL)
        funcSpec->AddHandler (&m_socketConnectHandler);

    try
    {
        LoadPlugins();
    }
    catch (Error &e)
    {
        GetLogger()->LogError("LoadPlugins failed: %s", e.what());
        return;
    }
    catch (...)
    {
        GetLogger()->LogError("LoadPlugins failed: unknown error");
        return;
    }

    mgr->HookFunctions ();
}

void
Agent::UnInitialize()
{
    HookManager * hookManager = HookManager::Instance ();

    // Bring things down safely:
    //   1) Unhook functions and wait for function calls in progress to return
    //   2) Unload plugins
    //   3) Uninitialize Intercept++
    //   4) Get rid of the logger

    hookManager->UnhookFunctions ();

    UnloadPlugins ();

    InterceptPP::UnInitialize ();

    delete m_binaryLogger;
    m_binaryLogger = NULL;

    // Let the UI know that we're no longer active
    InterlockedDecrement (&m_capture->ActiveAgentCount);

    // Clean up the rest
    if (m_capture != NULL)
    {
        UnmapViewOfFile (m_capture);
        m_capture = NULL;
    }

    if (m_map != NULL)
    {
        CloseHandle (m_map);
        m_map = NULL;
    }
}

void
Agent::LoadPlugins ()
{
    OWString pluginDir = GetBinPath ();
    pluginDir.append (L"\\Plugins\\");

    WIN32_FIND_DATA findData;
    OWString matchStr = pluginDir + L"oSpyAgent_*.dll";
    HANDLE findHandle = FindFirstFile (matchStr.c_str (), &findData);
    if (findHandle == INVALID_HANDLE_VALUE)
        return;

    const char * processName = Util::Instance ()->GetProcessName ().c_str ();
    InterceptPP::Logging::Logger * logger = InterceptPP::GetLogger ();
    InterceptPP::HookManager * hookManager = InterceptPP::HookManager::Instance ();

    do
    {
        OWString pluginPath = pluginDir + findData.cFileName;

        HMODULE module = LoadLibrary (pluginPath.c_str ());
        if (module != NULL)
        {
            AgentPlugin * plugin = NULL;

            AgentPluginGetDescFunc pluginGetDesc =
              (AgentPluginGetDescFunc) GetProcAddress (module, "oSpyAgentPluginGetDesc");

            if (pluginGetDesc != NULL)
            {
                const AgentPluginDesc * desc = pluginGetDesc ();

                if (desc->ApiVersion == 1)
                {
                    plugin = desc->CreateFunc ();
                    if (plugin != NULL)
                    {
                        plugin->Initialize (processName, logger, hookManager);

                        if (plugin->Open ())
                        {
                            m_plugins[desc] = plugin;
                            m_pluginModules.push_back (module);
                        }
                        else
                        {
                            desc->DestroyFunc (plugin);
                            plugin = NULL;
                        }
                    }
                }
            }

            if (plugin == NULL)
                FreeLibrary (module);
        }
    }
    while (FindNextFile (findHandle, &findData));

    FindClose(findHandle);
}

void
Agent::UnloadPlugins ()
{
    InterceptPP::HookManager * hookManager = InterceptPP::HookManager::Instance ();

    PluginMap::iterator pi;
    for (pi = m_plugins.begin (); pi != m_plugins.end (); pi++)
    {
        pi->second->Close ();
        pi->first->DestroyFunc (pi->second);
    }

    m_plugins.clear ();

    PluginModuleList::iterator mi;
    for (mi = m_pluginModules.begin (); mi != m_pluginModules.end (); mi++)
        FreeLibrary (*mi);

    m_pluginModules.clear ();
}

OWString
Agent::GetBinPath () const
{
    HMODULE moduleHandle = GetModuleHandle (OSPY_AGENT_MODULE_NAME);
    if (moduleHandle == NULL)
        throw Error("GetModuleHandle failed");

    WCHAR path[MAX_PATH];
    if (GetModuleFileName(moduleHandle, path, MAX_PATH) == 0)
        throw Error("GetModuleFileName failed");

    PathRemoveFileSpec(path);

    return path;
}

void
Agent::OnSocketConnect (FunctionCall * call, bool & shouldLog)
{
    if (call->GetState() != FUNCTION_CALL_ENTERING)
        return;

    char *argumentList = const_cast<char *>(call->GetArgumentsData().c_str());
    struct sockaddr_in *peerAddr = *reinterpret_cast<struct sockaddr_in **>(argumentList + sizeof(SOCKET));
    if (peerAddr->sin_family != AF_INET)
        return;

    DWORD retval, lastError;
    if (!HaveMatchingSoftwallRule("connect", call->GetReturnAddress(),
        NULL, peerAddr, retval, lastError))
    {
        return;
    }

    call->SetShouldCarryOn(false);
    call->GetCpuContextLive()->eax = retval;
    *(call->GetLastErrorLive()) = lastError;
}

bool
Agent::HaveMatchingSoftwallRule(const OString &functionName,
                                void *returnAddress,
                                const sockaddr_in *localAddress,
                                const sockaddr_in *peerAddress,
                                DWORD &retval,
                                DWORD &lastError)
{
    //GetLogger()->LogDebug("Checking %s against %d rules", functionName.c_str(), m_capture->NumSoftwallRules);

    for (unsigned int i = 0; i < m_capture->NumSoftwallRules; i++)
    {
        SoftwallRule *rule = &m_capture->Rules[i];

        if ((rule->Conditions & SOFTWALL_CONDITION_PROCESS_NAME) != 0)
        {
            if (m_processName != rule->ProcessName)
                continue;
        }

        if ((rule->Conditions & SOFTWALL_CONDITION_FUNCTION_NAME) != 0)
        {
            if (functionName != rule->FunctionName)
                continue;
        }

        if ((rule->Conditions & SOFTWALL_CONDITION_RETURN_ADDRESS) != 0)
        {
            if (returnAddress != rule->ReturnAddress)
                continue;
        }
        
        if ((rule->Conditions & SOFTWALL_CONDITION_LOCAL_ADDRESS) != 0)
        {
            if (localAddress == NULL ||
                memcmp(&localAddress->sin_addr, &rule->LocalAddress, sizeof(in_addr)) != 0)
            {
                continue;
            }
        }

        if ((rule->Conditions & SOFTWALL_CONDITION_LOCAL_PORT) != 0)
        {
            if (localAddress == NULL || localAddress->sin_port != rule->LocalPort)
            {
                continue;
            }
        }

        if ((rule->Conditions & SOFTWALL_CONDITION_PEER_ADDRESS) != 0)
        {
            if (peerAddress == NULL ||
                memcmp(&peerAddress->sin_addr, &rule->PeerAddress, sizeof(in_addr)) != 0)
            {
                continue;
            }
        }

        if ((rule->Conditions & SOFTWALL_CONDITION_PEER_PORT) != 0)
        {
            if (peerAddress == NULL || peerAddress->sin_port != rule->PeerPort)
            {
                continue;
            }
        }

        retval = rule->Retval;
        lastError = rule->LastError;

        //GetLogger()->LogDebug("Matched, setting retval to %d and lastError to %d", retval, lastError);
        return true;
    }

    //GetLogger()->LogDebug("No match");
    return false;
}

ULONG
Agent::GetNextLogIndex ()
{
  ULONG index = InterlockedIncrement (reinterpret_cast<volatile LONG *> (&m_capture->LogIndexUserspace));
  InterlockedIncrement (reinterpret_cast<volatile LONG *> (&m_capture->LogCount));
  return index;
}

ULONG
Agent::AddBytesLogged (ULONG n)
{
  ULONG prevSize = InterlockedExchangeAdd (reinterpret_cast<volatile LONG *> (&m_capture->LogSize), n);

  return prevSize + n;
}

} // namespace oSpy

extern "C" {

static DWORD WINAPI
AgentWorkerFunc (void * arg)
{
    ReentranceProtector protector;

    // Just to make sure that floating point support is dynamically loaded...
    float dummy_float = 1.0f;

    // And to make sure that the compiler doesn't optimize the previous statement out.
    if (dummy_float == 0.0f)
        return 1;

    // Grab an extra reference
    HMODULE moduleHandle = LoadLibrary (OSPY_AGENT_MODULE_NAME);
    HANDLE startReqEvent = NULL;
    HANDLE stopReqEvent = NULL;
    HANDLE stopRespEvent = NULL;
    oSpy::Agent * agent = NULL;

    // Should not happen
    if (moduleHandle == NULL)
        goto beach;

    startReqEvent = CreateEvent (NULL, TRUE, FALSE, OSPY_AGENT_START_REQUEST_EVENT_NAME);
    if (startReqEvent == NULL || GetLastError () != ERROR_ALREADY_EXISTS)
    {
        MessageBoxA (NULL, "CreateEvent failed (start event doesn't exist?)", "oSpyAgent Error", MB_OK | MB_ICONERROR);
        goto beach;
    }

    stopReqEvent = CreateEvent (NULL, TRUE, FALSE, OSPY_AGENT_STOP_REQUEST_EVENT_NAME);
    if (stopReqEvent == NULL || GetLastError () != ERROR_ALREADY_EXISTS)
    {
        MessageBoxA (NULL, "CreateEvent failed (stop request event doesn't exist?)", "oSpyAgent Error", MB_OK | MB_ICONERROR);
        goto beach;
    }

    stopRespEvent = CreateEvent (NULL, FALSE, FALSE, OSPY_AGENT_STOP_RESPONSE_EVENT_NAME);
    if (stopRespEvent == NULL || GetLastError () != ERROR_ALREADY_EXISTS)
    {
        MessageBoxA (NULL, "CreateEvent failed (stop response event doesn't exist?)", "oSpyAgent Error", MB_OK | MB_ICONERROR);
        goto beach;
    }

    HANDLE events[2] = { startReqEvent, stopReqEvent };
    if (WaitForMultipleObjects (2, events, FALSE, INFINITE) != WAIT_OBJECT_0)
        goto beach;

    agent = new oSpy::Agent ();

    try
    {
        agent->Initialize ();
    }
    catch (Error &e)
    {
        MessageBoxA (NULL, e.what(), "oSpyAgent Error", MB_OK | MB_ICONERROR);
    }
    catch (...)
    {
        MessageBoxA (NULL, "Unknown error", "oSpyAgent Error", MB_OK | MB_ICONERROR);
    }

    WaitForSingleObject (stopReqEvent, INFINITE);

beach:
    if (agent != NULL)
    {
        agent->UnInitialize ();
        delete agent;
    }

    if (stopRespEvent != NULL)
    {
        SetEvent (stopRespEvent);
        CloseHandle (stopRespEvent);
    }

    if (stopReqEvent != NULL)
        CloseHandle (stopReqEvent);

    if (startReqEvent != NULL)
        CloseHandle (startReqEvent);

    if (moduleHandle != NULL)
        FreeLibraryAndExitThread (moduleHandle, 0);

    return 1;
}

BOOL APIENTRY
DllMain(HMODULE hModule,
        DWORD  ul_reason_for_call,
        LPVOID lpReserved)
{
    ReentranceProtector protector;

    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls (hModule);

        HANDLE workerThread = CreateThread (NULL, 0, AgentWorkerFunc, NULL, 0, NULL);
        if (workerThread != NULL)
            CloseHandle (workerThread);
        else
            MessageBoxA (NULL, "CreateThread failed", "oSpyAgent Error", MB_OK | MB_ICONERROR);
    }

    return TRUE;
}

} // extern "C"

#ifdef _MANAGED
#pragma managed(pop)
#endif
