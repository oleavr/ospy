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

#include "OpenGL.hpp"

using namespace System;

namespace oSpyAgent
{
    namespace Hooks
    {
        GLMonitor::GLMonitor()
        {
            String ^oglDll = _T("Opengl32.dll");

            IntPtr ptr = LocalHook::GetProcAddress(oglDll, _T("wglMakeCurrent"));
            wglMakeCurrentImpl = static_cast<WglMakeCurrentFunc>(static_cast<void *>(ptr));
            wglMakeCurrentHook = LocalHook::Create(
                ptr,
                gcnew WglMakeCurrentHandler(this, &GLMonitor::OnWglMakeCurrent),
                this);

            array<int> ^anyAcl = gcnew array<int>(1);
            anyAcl[0] = 0;
            wglMakeCurrentHook->ThreadACL->SetExclusiveACL(anyAcl);
        }

        BOOL GLMonitor::OnWglMakeCurrent(HDC hdc, HGLRC hglrc)
        {
            Event::InvocationOrigin origin("wglMakeCurrent", BacktraceHere(), UInt32(hglrc));
            MessageEvent ^ev = gcnew MessageEvent(factory, origin, "hdc=0x{0:x8} hglrc=0x{1:x8}", UInt32(hdc), UInt32(hglrc));
            logger->Submit(ev);
            return wglMakeCurrentImpl(hdc, hglrc);
        }
    }
}
