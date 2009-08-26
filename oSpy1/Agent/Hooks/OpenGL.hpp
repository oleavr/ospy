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

#include "Monitor.hpp"

namespace oSpyAgent
{
    namespace Hooks
    {
        public ref class GLMonitor : Monitor
        {
        public:
            GLMonitor();

        private:
            DECLARE_HOOK(int, ChoosePixelFormat, HDC hdc, const PIXELFORMATDESCRIPTOR *ppfd);
            DECLARE_HOOK(int, DescribePixelFormat, HDC hdc, int iPixelFormat, UINT nBytes, LPPIXELFORMATDESCRIPTOR ppfd);
            DECLARE_HOOK(int, GetPixelFormat, HDC hdc);
            DECLARE_HOOK(BOOL, SetPixelFormat, HDC hdc, int iPixelFormat, const PIXELFORMATDESCRIPTOR *ppfd);
            DECLARE_HOOK(BOOL, SwapBuffers, HDC hdc);
            DECLARE_HOOK(BOOL, wglMakeCurrent, HDC hdc, HGLRC hglrc);
        };
    }
}
