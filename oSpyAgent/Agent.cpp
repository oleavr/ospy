//
// Copyright (c) 2006-2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
#include "Agent.h"

#ifdef _MANAGED
#pragma managed(push, off)
#endif

static HMODULE g_mod = NULL;
static Agent *g_agent = NULL;

Agent::Agent()
    : m_map(INVALID_HANDLE_VALUE),
      m_capture(NULL)
{
}

void
Agent::Initialize()
{
    InterceptPP::Initialize();

    m_map = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, "oSpyCapture");
    m_capture = static_cast<Capture *>(MapViewOfFile(m_map, FILE_MAP_WRITE, 0, 0, sizeof(Capture)));
    if (m_capture == NULL)
        throw Error("MapViewOfFile failed");

    // Create the logger and tell Intercept++ to use it
    {
        OOWStringStream ss;
        ss << m_capture->LogPath << "\\" << GetProcessId(GetCurrentProcess()) << ".log";

        InterceptPP::SetLogger(new BinaryLogger(this, ss.str()));
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
	FunctionSpec *funcSpec = mgr->GetFunctionSpecById("connect");
    if (funcSpec != NULL)
	    funcSpec->SetHandler(OnSocketConnectWrapper, this);

#ifdef RESEARCH_MODE
    funcSpec = mgr->GetFunctionSpecById("WaitForSingleObject");
    if (funcSpec != NULL)
        funcSpec->SetHandler(OnWaitForSingleObject, this);

    funcSpec = mgr->GetFunctionSpecById("WaitForSingleObjectEx");
    if (funcSpec != NULL)
        funcSpec->SetHandler(OnWaitForSingleObject, this);

    funcSpec = mgr->GetFunctionSpecById("WaitForMultipleObjects");
    if (funcSpec != NULL)
        funcSpec->SetHandler(OnWaitForMultipleObjects, this);

    funcSpec = mgr->GetFunctionSpecById("WaitForMultipleObjectsEx");
    if (funcSpec != NULL)
        funcSpec->SetHandler(OnWaitForMultipleObjects, this);

    if (m_processName == "msnmsgr.exe")
    {
        funcSpec = new FunctionSpec("CP2PTransport::SendControlPacket", CALLING_CONV_STDCALL, 12);
        Function *func = new Function(funcSpec, 0x4EDD4A);
        func->Hook();
    }
#endif
}

void
Agent::OnSocketConnectWrapper(FunctionCall *call, void *userData, bool &shouldLog)
{
	Agent *self = static_cast<Agent *>(userData);
	self->OnSocketConnect(call);
}

void
Agent::OnSocketConnect(FunctionCall *call)
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

#if 0
    const void *argumentList = call->GetArgumentsData().c_str();
    SOCKET s = *static_cast<const SOCKET *>(argumentList);    

    struct sockaddr_in localAddr, peerAddr;
    int sinLen;

    sinLen = sizeof(localAddr);
    getsockname(s, reinterpret_cast<struct sockaddr *>(&localAddr), &sinLen);

    sinLen = sizeof(peerAddr);
    getpeername(s, reinterpret_cast<struct sockaddr *>(&peerAddr), &sinLen);
#endif

#ifdef RESEARCH_MODE

void
Agent::OnWaitForSingleObject(FunctionCall *call, void *userData, bool &shouldLog)
{
    char *argumentList = const_cast<char *>(call->GetArgumentsData().c_str());
    DWORD timeout = *reinterpret_cast<DWORD *>(argumentList + sizeof(HANDLE));

    shouldLog = (timeout != 0 && timeout != INFINITE);
}

void
Agent::OnWaitForMultipleObjects(FunctionCall *call, void *userData, bool &shouldLog)
{
    char *argumentList = const_cast<char *>(call->GetArgumentsData().c_str());
    DWORD timeout = *reinterpret_cast<DWORD *>(argumentList + sizeof(DWORD) + sizeof(DWORD *) + sizeof(BOOL));

    shouldLog = (timeout != 0 && timeout != INFINITE);
}

#endif

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
        SoftwallRule *rule = &m_capture->rules[i];

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

void
Agent::UnInitialize()
{
    // Uninitialize Intercept++, effectively unhooking everything
    InterceptPP::UnInitialize();

    // Clean up the rest
    if (m_capture != NULL)
    {
        UnmapViewOfFile(m_capture);
        m_capture = NULL;
    }

    if (m_map != NULL)
    {
        CloseHandle(m_map);
        m_map = NULL;
    }
}

LONG
Agent::GetNextLogIndex()
{
    return InterlockedIncrement(&m_capture->LogIndex);
}

LONG
Agent::AddBytesLogged(LONG n)
{
    LONG prevSize = InterlockedExchangeAdd(&m_capture->LogSize, n);

    return prevSize + n;
}

BOOL APIENTRY
DllMain(HMODULE hModule,
        DWORD  ul_reason_for_call,
        LPVOID lpReserved)
{
    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        //__asm int 3;

		// Just to make sure that floating point support is dynamically loaded...
		float dummy_float = 1.0f;

		// And to make sure that the compiler doesn't optimize the previous statement out.
		if (dummy_float > 0.0f)
		{
			g_mod = hModule;
            g_agent = new Agent();
            g_agent->Initialize();
		}
    }
    else if (ul_reason_for_call == DLL_PROCESS_DETACH)
    {
        if (g_agent != NULL)
        {
            g_agent->UnInitialize();
            delete g_agent;
            g_agent = NULL;
        }

        g_mod = NULL;
    }

    return TRUE;
}

#ifdef _MANAGED
#pragma managed(pop)
#endif
