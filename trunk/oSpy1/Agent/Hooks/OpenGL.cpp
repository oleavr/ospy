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
            String ^gdiDll = _T("gdi32.dll");

            array<int> ^anyAcl = gcnew array<int>(1);
            anyAcl[0] = 0;

            INSTALL_HOOK(gdiDll, ChoosePixelFormat);
            INSTALL_HOOK(gdiDll, DescribePixelFormat);
            INSTALL_HOOK(gdiDll, GetPixelFormat);
            INSTALL_HOOK(gdiDll, SetPixelFormat);
            INSTALL_HOOK(gdiDll, SwapBuffers);
            INSTALL_HOOK(oglDll, wglMakeCurrent);
        }

        int GLMonitor::OnChoosePixelFormat(HDC hdc, const PIXELFORMATDESCRIPTOR *ppfd)
        {
            AutoSubmitMessage msg(this, "ChoosePixelFormat", UInt32(hdc));
            int result = ChoosePixelFormatImpl(hdc, ppfd);
            msg->SetMessage("hdc=0x{0:x8} => {1}", UInt32(hdc), Int32(result));
            return result;
        }

        int GLMonitor::OnDescribePixelFormat(HDC hdc, int iPixelFormat, UINT nBytes, LPPIXELFORMATDESCRIPTOR ppfd)
        {
            AutoSubmitMessage msg(this, "DescribePixelFormat", UInt32(hdc));
            int result = DescribePixelFormatImpl(hdc, iPixelFormat, nBytes, ppfd);
            msg->SetMessage("hdc=0x{0:x8} iPixelFormat={1} nBytes={2} => {3}",
                UInt32(hdc), Int32(iPixelFormat), UInt32(nBytes), Int32(result));
            return result;
        }

        int GLMonitor::OnGetPixelFormat(HDC hdc)
        {
            AutoSubmitMessage msg(this, "GetPixelFormat", UInt32(hdc));
            int result = GetPixelFormatImpl(hdc);
            msg->SetMessage("hdc=0x{0:x8} => {1}", UInt32(hdc), Int32(result));
            return result;
        }

        BOOL GLMonitor::OnSetPixelFormat(HDC hdc, int iPixelFormat, const PIXELFORMATDESCRIPTOR *ppfd)
        {
            AutoSubmitMessage msg(this, "SetPixelFormat", UInt32(hdc));
            BOOL result = SetPixelFormatImpl(hdc, iPixelFormat, ppfd);
            msg->SetMessage("hdc=0x{0:x8} iPixelFormat={1} => {2}",
                UInt32(hdc), Int32(iPixelFormat), BoolToString(result));
            return result;
        }

        BOOL GLMonitor::OnSwapBuffers(HDC hdc)
        {
            AutoSubmitMessage msg(this, "SwapBuffers", UInt32(hdc));
            BOOL result = SwapBuffersImpl(hdc);
            msg->SetMessage("hdc=0x{0:x8} => {1}", UInt32(hdc), BoolToString(result));
            return result;
        }

        BOOL GLMonitor::OnwglMakeCurrent(HDC hdc, HGLRC hglrc)
        {
            AutoSubmitMessage msg(this, "wglMakeCurrent", UInt32(hglrc));
            BOOL result = wglMakeCurrentImpl(hdc, hglrc);
            msg->SetMessage("hdc=0x{0:x8} hglrc=0x{1:x8} => {2}", UInt32(hdc), UInt32(hglrc), BoolToString(result));
            return result;
        }
    }
}
