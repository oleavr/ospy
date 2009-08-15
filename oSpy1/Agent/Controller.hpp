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

#include "logging.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace EasyHook;

namespace oSpyAgent
{
    private delegate void SubmitElementHandler(const MessageQueueElement *el);

    public ref class Controller : public IEntryPoint
    {
    public:
        Controller(RemoteHooking::IContext ^context, String ^channelName, array<oSpy::Capture::SoftwallRule ^> ^softwallRules);

        void Run(RemoteHooking::IContext ^context, String ^channelName, array<oSpy::Capture::SoftwallRule ^> ^softwallRules);

    private:
        void EnableLegacyHooks();
        void OnSubmitElement(const MessageQueueElement *el);

        oSpy::Capture::IManager ^m_manager;
        array<oSpy::Capture::SoftwallRule ^> ^m_softwallRules;
        List<oSpy::Capture::MessageQueueElement ^> m_events;

        SubmitElementHandler ^m_submitElementHandler;
    };
}
