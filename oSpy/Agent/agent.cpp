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
#include "BinaryLogger.h"
#include "logging_old.h"
#include "hooks.h"
#include "util.h"
#include "overlapped.h"

#ifdef _MANAGED
#pragma managed(push, off)
#endif

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
			// Initialize SHM logger
			message_logger_init();

            //COverlappedManager::Init();

            InterceptPP::Initialize();

            OModuleInfo mi = Util::Instance()->GetModuleInfo(DllMain);
            OString ourDir = Util::Instance()->GetDirectory(mi);

            //InterceptPP::SetLogger(new BinaryLogger(ourDir + "\\oSpyAgentLog.bin"));
            InterceptPP::SetLogger(new BinaryLogger("C:\\oSpyAgentLog.bin"));

            HookManager *mgr = HookManager::Instance();
            try
            {
                //mgr->LoadDefinitions(ourDir + "\\config.xml");
                mgr->LoadDefinitions("D:\\Projects\\oSpy\\trunk\\oSpy\\Agent\\config.xml");
            }
            catch (Error &e)
            {
                GetLogger()->LogError("LoadDefinitions failed: %s", e.what());
            }
            catch (...)
            {
                GetLogger()->LogError("LoadDefinitions failed: unknown error");
            }

			//hook_kernel32();
			hook_winsock();
			hook_secur32();
			hook_crypt();
			hook_wininet();
			//hook_httpapi();
			hook_activesync();
			//hook_msn();
		}
    }

    return TRUE;
}

#ifdef _MANAGED
#pragma managed(pop)
#endif
