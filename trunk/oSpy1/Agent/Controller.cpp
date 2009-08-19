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

#include "Stdafx.h"

#include "Controller.hpp"

#include "hooks.h"
#include "hooking.h"
#include "util.h"

#include <msclr\lock.h>

using namespace System::Runtime::InteropServices;
using namespace System::Threading;

namespace oSpyAgent
{
    Controller::Controller(RemoteHooking::IContext ^context,
                           String ^channelName,
                           array<oSpy::Capture::SoftwallRule ^> ^softwallRules)
        : m_events(gcnew List<oSpy::Capture::MessageQueueElement ^>())
    {
        String ^url = "ipc://" + channelName + "/" + channelName;
        m_manager = dynamic_cast<oSpy::Capture::IManager ^>(Activator::GetObject(oSpy::Capture::IManager::typeid, url));

        m_softwallRules = softwallRules;

        m_submitElementHandler = gcnew SubmitElementHandler(this, &Controller::OnSubmitElement);
        m_submitElementHandlerFuncPtr = Marshal::GetFunctionPointerForDelegate(m_submitElementHandler);
    }

    void Controller::Run(RemoteHooking::IContext ^context,
                         String ^channelName,
                         array<oSpy::Capture::SoftwallRule ^> ^softwallRules)
    {
        EnableLegacyHooks();

        RemoteHooking::WakeUpProcess();

        try
        {
            while (true)
            {
                Thread::Sleep(500);

                if (m_events.Count > 0)
                {
                    array<oSpy::Capture::MessageQueueElement ^> ^elements = nullptr;

                    {
                        msclr::lock l(this);
                        elements = m_events.ToArray();
                        m_events.Clear();
                    }

                    m_manager->Submit(elements);
                }
                else
                {
                    m_manager->Ping();
                }
            }
        }
        catch (Exception ^)
        {
        }

        DisableLegacyHooks();
    }

    void Controller::EnableLegacyHooks()
    {
        HookManager::Init();
        CUtil::Init();

        SoftwallRule *rules = new SoftwallRule[m_softwallRules->Length];
        unsigned int i = 0;
        for each (oSpy::Capture::SoftwallRule ^rule in m_softwallRules)
        {
            IntPtr nativeRule(&rules[i]);
            Marshal::StructureToPtr(rule, nativeRule, false);
            i++;
        }
        softwall_init(rules, m_softwallRules->Length);
        delete[] rules;

        message_logger_init(static_cast<MessageLoggerSubmitFunc>(static_cast<void *>(m_submitElementHandlerFuncPtr)));

        hook_winsock();
        hook_secur32();
        hook_crypt();
        hook_wininet();
        hook_activesync();
        hook_msn();
    }

    void Controller::DisableLegacyHooks()
    {
        HookManager *mgr = HookManager::Obtain();
        mgr->RemoveAll();
        mgr->CloseLibraries();

        HookManager::Uninit();
    }

    void Controller::OnSubmitElement(const MessageQueueElement *el)
    {
        IntPtr elPtr(const_cast<MessageQueueElement *>(el));
        oSpy::Capture::MessageQueueElement ^managedEl = gcnew oSpy::Capture::MessageQueueElement;
        Marshal::PtrToStructure(elPtr, managedEl);

        msclr::lock l(this);
        m_events.Add(managedEl);
    }
}
