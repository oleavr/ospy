//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace oSpy
{
    public class GroovyRichTextBox : RichTextBox
    {
        public GroovyRichTextBox()
        {
            WordWrap = false;
            ReadOnly = true;
            BorderStyle = BorderStyle.None;
            ScrollBars = RichTextBoxScrollBars.Vertical;
            AutoSize = false;
            DetectUrls = false;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr LoadLibrary(string lpFileName);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams prams = base.CreateParams;

                if (LoadLibrary("msftedit.dll") != IntPtr.Zero)
                {
                    //prams.ExStyle |= 0x20; // transparent
                    prams.ClassName = "RICHEDIT50W";
                }

                return prams;
            }
        }
    }
}
