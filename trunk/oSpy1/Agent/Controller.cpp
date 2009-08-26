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

#using <System.dll>

#include "Controller.hpp"

#include "hooks.h"
#include "hooking.h"
#include "util.h"

#include "Hooks/OpenGL.hpp"

#include <msclr\lock.h>

using namespace System::Runtime::InteropServices;
using namespace System::Threading;

namespace oSpyAgent
{
    Controller::Controller(RemoteHooking::IContext ^context,
                           String ^channelName,
                           array<oSpy::Capture::SoftwallRule ^> ^softwallRules)
        : eventCoordinator(gcnew oSpy::Capture::EventCoordinator()),
          events(gcnew List<oSpy::Capture::Event ^>())
    {
        String ^url = "ipc://" + channelName + "/" + channelName;
        this->manager = dynamic_cast<oSpy::Capture::IManager ^>(Activator::GetObject(oSpy::Capture::IManager::typeid, url));

        this->softwallRules = softwallRules;

        this->submitElementHandler = gcnew SubmitElementHandler(this, &Controller::OnSubmitElement);
        this->submitElementHandlerFuncPtr = Marshal::GetFunctionPointerForDelegate(submitElementHandler);
    }

    void Controller::Run(RemoteHooking::IContext ^context,
                         String ^channelName,
                         array<oSpy::Capture::SoftwallRule ^> ^softwallRules)
    {
        EnableLegacyHooks();

        try
        {
            Hooks::GLMonitor glm;
            glm.SetLogger(this);
        }
        catch (...)
        {
        }

        RemoteHooking::WakeUpProcess();

        DWORD myPid = GetCurrentProcessId();

        try
        {
            while (true)
            {
                Thread::Sleep(500);

                if (events.Count > 0)
                {
                    array<oSpy::Capture::Event ^> ^batch = nullptr;

                    {
                        msclr::lock l(this);
                        batch = events.ToArray();
                        events.Clear();
                    }

                    manager->Submit(batch);
                }
                else
                {
                    manager->Ping(myPid);
                }
            }
        }
        catch (Exception ^ex)
        {
#ifdef _DEBUG
            pin_ptr<const wchar_t> str = PtrToStringChars(ex->Message);
            MessageBox(NULL, str, _T("Error"), MB_OK | MB_ICONERROR);
#endif
        }

        DisableLegacyHooks();
    }

    oSpy::Capture::EventCoordinator ^Controller::Coordinator::get()
    {
        return eventCoordinator;
    }

    void Controller::Submit(oSpy::Capture::Event ^ev)
    {
        msclr::lock l(this);
        events.Add(ev);
    }

    void Controller::EnableLegacyHooks()
    {
        HookManager::Init();
        CUtil::Init();

        ::SoftwallRule *rules = new ::SoftwallRule[softwallRules->Length];
        unsigned int i = 0;
        for each (oSpy::Capture::SoftwallRule ^rule in softwallRules)
        {
            IntPtr nativeRule(&rules[i]);
            Marshal::StructureToPtr(rule, nativeRule, false);
            i++;
        }
        softwall_init(rules, softwallRules->Length);
        delete[] rules;

        message_logger_init(static_cast<MessageLoggerSubmitFunc>(static_cast<void *>(submitElementHandlerFuncPtr)));

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
        mgr->Shutdown();

        HookManager::Uninit();
    }

    void Controller::OnSubmitElement(const MessageQueueElement *el)
    {
        Event ^ev = nullptr;

        Event::InvocationOrigin origin(gcnew String(el->function_name), gcnew String(el->backtrace), el->resource_id);

        if (el->type == MESSAGE_TYPE_MESSAGE)
        {
            MessageEvent ^msgEv = gcnew MessageEvent(eventCoordinator, origin, gcnew String(el->message));
            msgEv->Context = static_cast<oSpy::MessageContext>(el->context);

            ev = msgEv;
        }
        else
        {
            PacketEvent ^pktEv = gcnew PacketEvent(eventCoordinator, origin);

            ev = pktEv;
        }

        ev->Direction = static_cast<oSpy::PacketDirection>(el->direction);

        if (el->local_address.sin_port != 0)
        {
            ev->LocalEndpoint = gcnew System::Net::IPEndPoint(Int64(el->local_address.sin_addr.s_addr), ntohs(el->local_address.sin_port));
        }

        if (el->peer_address.sin_port != 0)
        {
            ev->PeerEndpoint = gcnew System::Net::IPEndPoint(Int64(el->peer_address.sin_addr.s_addr), ntohs(el->peer_address.sin_port));
        }

        if (el->len > 0)
        {
            array<Byte> ^data = gcnew array<Byte>(el->len);
            Marshal::Copy(static_cast<IntPtr>(const_cast<char *>(el->buf)), data, 0, el->len);
            ev->Data = data;
        }

        Submit(ev);
    }
}
