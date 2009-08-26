//
// Copyright (C) 2009  Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

#pragma once

#using <EasyHook.dll>
#using <oSpy.exe>

#include "IEventLogger.hpp"

using namespace System;
using namespace EasyHook;
using namespace oSpy::Capture;

namespace oSpyAgent
{
    namespace Hooks
    {
        #define DECLARE_HOOK(RTYPE, FNAME, ...)                                     \
            typedef RTYPE (WINAPI * FNAME##Func)(__VA_ARGS__);                      \
            delegate RTYPE FNAME##Handler(__VA_ARGS__);                             \
            FNAME##Func FNAME##Impl;                                                \
            LocalHook ^FNAME##Hook;                                                 \
            RTYPE On##FNAME(__VA_ARGS__)

        #define INSTALL_HOOK(MOD, FNAME)                                            \
            {                                                                       \
                IntPtr ptr = LocalHook::GetProcAddress(MOD, _T(#FNAME));            \
                FNAME##Impl = static_cast<FNAME##Func>(static_cast<void *>(ptr));   \
                FNAME##Hook = LocalHook::Create(ptr,                                \
                    gcnew FNAME##Handler(this, &GLMonitor::On##FNAME),              \
                    this);                                                          \
                FNAME##Hook->ThreadACL->SetExclusiveACL(anyAcl);                    \
            }

        public ref class Monitor
        {
        public:
            void SetLogger(IEventLogger ^logger);

        internal:
            String ^BacktraceHere();
            String ^BoolToString(BOOL value);

            IEventLogger ^logger;
            EventCoordinator ^coordinator;
        };

        public ref class AutoSubmitMessage
        {
        public:
            AutoSubmitMessage(Monitor ^monitor, String ^functionName, UInt32 resourceId)
                : monitor(monitor)
            {
                Event::InvocationOrigin origin(functionName, monitor->BacktraceHere(), resourceId);
                ev = gcnew MessageEvent(monitor->coordinator, origin);
            }

            ~AutoSubmitMessage()
            {
                monitor->logger->Submit(ev);
            }

            MessageEvent ^operator->()
            {
                return ev;
            }

        private:
            Monitor ^monitor;
            MessageEvent ^ev;
        };
    }
}
