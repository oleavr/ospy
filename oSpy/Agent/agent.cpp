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
      m_capture(NULL),
      m_stoppedEvent(INVALID_HANDLE_VALUE)
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

    m_stoppedEvent = OpenEvent(EVENT_ALL_ACCESS, FALSE, "oSpyCaptureStopped");
    if (m_stoppedEvent == NULL)
        throw Error("OpenEvent failed");

    // Register
    InterlockedIncrement(&m_capture->ClientCount);

    // Create the logger and tell Intercept++ to use it
    {
        OOWStringStream ss;
        ss << m_capture->LogPath << "\\" << GetProcessId(GetCurrentProcess()) << ".log";

        InterceptPP::SetLogger(new BinaryLogger(this, ss.str()));
    }

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
    }
    catch (...)
    {
        GetLogger()->LogError("LoadDefinitions failed: unknown error");
    }

    // Start a monitoring thread that does unhooking etc. when the trace is stopped
    CreateThread(NULL, 0, MonitorThreadFuncWrapper, this, 0, NULL);
}

void
Agent::UnInitialize()
{
    // Uninitialize Intercept++, effectively unhooking everything
    InterceptPP::UnInitialize();

    // Unregister
    InterlockedDecrement(&m_capture->ClientCount);

    // Clean up the rest
    if (m_stoppedEvent != NULL)
    {
        CloseHandle(m_stoppedEvent);
        m_stoppedEvent = NULL;
    }

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

DWORD WINAPI
Agent::MonitorThreadFuncWrapper(LPVOID param)
{
    Agent *instance = reinterpret_cast<Agent *>(param);

    instance->MonitorThreadFunc();

    CreateThread(NULL, 0, reinterpret_cast<LPTHREAD_START_ROUTINE>(FreeLibrary), g_mod, 0, NULL);

    return 0;
}

void
Agent::MonitorThreadFunc()
{
    // Wait for the stopped event
    while (WaitForSingleObject(m_stoppedEvent, INFINITE) != WAIT_OBJECT_0);

    // Clean up
    g_agent->UnInitialize();
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
