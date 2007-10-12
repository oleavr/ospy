//
// Copyright (c) 2006-2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
