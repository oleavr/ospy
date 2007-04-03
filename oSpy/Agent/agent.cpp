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

Agent::Agent()
{
    InterceptPP::Initialize();
}

void
Agent::Attach()
{
    HANDLE map = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, "oSpyCaptureConfig");
    m_cfg = static_cast<CaptureConfig *>(MapViewOfFile(map, FILE_MAP_WRITE, 0, 0, sizeof(CaptureConfig)));

    // Create the logger and tell Intercept++ to use it
    {
        OOWStringStream ss;
        ss << m_cfg->LogPath << "\\" << GetProcessId(GetCurrentProcess()) << ".log";

        m_logger = new BinaryLogger(this, ss.str());
        InterceptPP::SetLogger(m_logger);
    }

    // Load hook definitions from XML
    HookManager *mgr = HookManager::Instance();
    try
    {
        OOWStringStream ss;
        ss << m_cfg->LogPath << "\\" << "config.xml";
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

LONG
Agent::GetNextLogIndex()
{
    return InterlockedIncrement(&m_cfg->LogIndex);
}

LONG
Agent::AddBytesLogged(LONG n)
{
    LONG prevSize = InterlockedExchangeAdd(&m_cfg->LogSize, n);

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
            Agent *agent = new Agent();
            agent->Attach();
		}
    }

    return TRUE;
}

#ifdef _MANAGED
#pragma managed(pop)
#endif
