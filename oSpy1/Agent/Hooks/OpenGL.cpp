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
using namespace System::Text;

namespace oSpyAgent
{
    namespace Hooks
    {
        #define CRLF "\r\n"

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
            AutoSubmitPacket pkt(this, "ChoosePixelFormat", UInt32(hdc));
            int result = ChoosePixelFormatImpl(hdc, ppfd);
            pkt->SetMessage("hdc=0x{0:x8} => {1}", UInt32(hdc), Int32(result));
            if (ppfd != NULL)
                pkt->Data = PixelFormatDescriptorToRawString(ppfd);
            return result;
        }

        int GLMonitor::OnDescribePixelFormat(HDC hdc, int iPixelFormat, UINT nBytes, LPPIXELFORMATDESCRIPTOR ppfd)
        {
            AutoSubmitPacket pkt(this, "DescribePixelFormat", UInt32(hdc));
            int result = DescribePixelFormatImpl(hdc, iPixelFormat, nBytes, ppfd);
            pkt->SetMessage("hdc=0x{0:x8} iPixelFormat={1} nBytes={2} => {3}",
                UInt32(hdc), Int32(iPixelFormat), UInt32(nBytes), Int32(result));
            if (result != 0 && ppfd != NULL)
                pkt->Data = PixelFormatDescriptorToRawString(ppfd);
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
            AutoSubmitPacket pkt(this, "SetPixelFormat", UInt32(hdc));
            BOOL result = SetPixelFormatImpl(hdc, iPixelFormat, ppfd);
            pkt->SetMessage("hdc=0x{0:x8} iPixelFormat={1} => {2}",
                UInt32(hdc), Int32(iPixelFormat), BoolToString(result));
            if (ppfd != NULL)
                pkt->Data = PixelFormatDescriptorToRawString(ppfd);
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

        array<unsigned char> ^GLMonitor::PixelFormatDescriptorToRawString(const PIXELFORMATDESCRIPTOR *pfd)
        {
            StringBuilder sb;

            if (pfd->nSize == sizeof(PIXELFORMATDESCRIPTOR))
                sb.Append("nSize: sizeof(PIXELFORMATDESCRIPTOR)" CRLF);
            else
                sb.AppendFormat("nSize: {0}" CRLF, pfd->nSize);
            sb.AppendFormat("nVersion: {0}" CRLF, pfd->nVersion);
            sb.AppendFormat("dwFlags: {0}" CRLF, PixelFormatFlagsToString(pfd->dwFlags));
            sb.AppendFormat("iPixelType: {0}" CRLF, PixelTypeToString(pfd->iPixelType));
            sb.AppendFormat("cColorBits: {0}" CRLF, pfd->cColorBits);
            sb.AppendFormat("cRedBits: {0}" CRLF, pfd->cRedBits);
            sb.AppendFormat("cRedShift: {0}" CRLF, pfd->cRedShift);
            sb.AppendFormat("cGreenBits: {0}" CRLF, pfd->cGreenBits);
            sb.AppendFormat("cGreenShift: {0}" CRLF, pfd->cGreenShift);
            sb.AppendFormat("cBlueBits: {0}" CRLF, pfd->cBlueBits);
            sb.AppendFormat("cBlueShift: {0}" CRLF, pfd->cBlueShift);
            sb.AppendFormat("cAlphaBits: {0}" CRLF, pfd->cAlphaBits);
            sb.AppendFormat("cAlphaShift: {0}" CRLF, pfd->cAlphaShift);
            sb.AppendFormat("cAccumBits: {0}" CRLF, pfd->cAccumBits);
            sb.AppendFormat("cAccumRedBits: {0}" CRLF, pfd->cAccumRedBits);
            sb.AppendFormat("cAccumGreenBits: {0}" CRLF, pfd->cAccumGreenBits);
            sb.AppendFormat("cAccumBlueBits: {0}" CRLF, pfd->cAccumBlueBits);
            sb.AppendFormat("cAccumAlphaBits: {0}" CRLF, pfd->cAccumAlphaBits);
            sb.AppendFormat("cDepthBits: {0}" CRLF, pfd->cDepthBits);
            sb.AppendFormat("cStencilBits: {0}" CRLF, pfd->cStencilBits);
            sb.AppendFormat("cAuxBuffers: {0}" CRLF, pfd->cAuxBuffers);
            sb.AppendFormat("iLayerType: {0}" CRLF, LayerTypeToString(pfd->iLayerType));
            sb.AppendFormat("bReserved: {0}" CRLF, pfd->bReserved);
            sb.AppendFormat("dwLayerMask: {0}" CRLF, pfd->dwLayerMask);
            sb.AppendFormat("dwVisibleMask: {0}" CRLF, pfd->dwVisibleMask);
            sb.AppendFormat("dwDamageMask: {0}" CRLF, pfd->dwDamageMask);

            return Encoding::UTF8->GetBytes(sb.ToString());
        }

        value struct Flag
        {
            DWORD Value;
            String ^Name;
        };

        #define DEFINE_FLAG(name) \
            { name, #name }

        String ^GLMonitor::PixelFormatFlagsToString(DWORD flags)
        {
            array<Flag> ^knownFlags =
            {
                DEFINE_FLAG(PFD_DOUBLEBUFFER),
                DEFINE_FLAG(PFD_STEREO),
                DEFINE_FLAG(PFD_DRAW_TO_WINDOW),
                DEFINE_FLAG(PFD_DRAW_TO_BITMAP),
                DEFINE_FLAG(PFD_SUPPORT_GDI),
                DEFINE_FLAG(PFD_SUPPORT_OPENGL),
                DEFINE_FLAG(PFD_GENERIC_FORMAT),
                DEFINE_FLAG(PFD_NEED_PALETTE),
                DEFINE_FLAG(PFD_NEED_SYSTEM_PALETTE),
                DEFINE_FLAG(PFD_SWAP_EXCHANGE),
                DEFINE_FLAG(PFD_SWAP_COPY),
                DEFINE_FLAG(PFD_SWAP_LAYER_BUFFERS),
                DEFINE_FLAG(PFD_GENERIC_ACCELERATED),
                DEFINE_FLAG(PFD_SUPPORT_DIRECTDRAW),
                DEFINE_FLAG(PFD_DIRECT3D_ACCELERATED),
                DEFINE_FLAG(PFD_SUPPORT_COMPOSITION),
                DEFINE_FLAG(PFD_DEPTH_DONTCARE),
                DEFINE_FLAG(PFD_DOUBLEBUFFER_DONTCARE),
                DEFINE_FLAG(PFD_STEREO_DONTCARE)
            };

            StringBuilder sb;
            DWORD tmp = flags;

            for each (Flag f in knownFlags)
            {
                if ((tmp & f.Value) == f.Value)
                {
                    if (sb.Length > 0)
                        sb.Append(" | ");
                    sb.Append(f.Name);
                    tmp &= ~f.Value;
                }
            }

            if (tmp != 0)
            {
                if (sb.Length > 0)
                    sb.Append(" | ");
                sb.AppendFormat("0x{0:x8}", tmp);
            }

            return sb.ToString();;
        }

        String ^GLMonitor::PixelTypeToString(BYTE pixelType)
        {
            switch (pixelType)
            {
                case PFD_TYPE_RGBA: return "PFD_TYPE_RGBA";
                case PFD_TYPE_COLORINDEX: return "PFD_TYPE_COLORINDEX";
                default: return "<unknown>";
            }
        }

        String ^GLMonitor::LayerTypeToString(BYTE layerType)
        {
            switch (layerType)
            {
                case PFD_MAIN_PLANE: return "PFD_MAIN_PLANE";
                case PFD_OVERLAY_PLANE: return "PFD_OVERLAY_PLANE";
                case PFD_UNDERLAY_PLANE: return "PFD_UNDERLAY_PLANE";
                default: return "<unknown>";
            }
        }
    }
}
