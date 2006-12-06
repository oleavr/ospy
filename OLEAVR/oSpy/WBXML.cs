/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

namespace oSpy
{
    class WBXML
    {
        static WBXML()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dllPath = Path.GetFullPath(@"libwbxml2.dll");
            IntPtr ret = LoadLibrary(dllPath);
            if (ret == IntPtr.Zero)
            {
                MessageBox.Show(
                    String.Format("Failed to load DLL: '{0}'", dllPath));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("libwbxml2.dll", EntryPoint = "wbxml_conv_wbxml2xml",
            CharSet = CharSet.Ansi)]
        protected static extern UInt32 wbxml_conv_wbxml2xml(
            byte[] wbxml, int wbxml_size, out string xml, ref WBXMLConvWBXML2XMLParams parms);

        protected const int WBXML_ENCODER_XML_GEN_COMPACT = 0;      // Compact XML generation
        protected const int WBXML_ENCODER_XML_GEN_INDENT = 1;       // Indented XML generation
        protected const int WBXML_ENCODER_XML_GEN_CANONICAL = 2;    // Canonical XML generation

        protected const int WBXML_LANG_AIRSYNC = 24;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        protected struct WBXMLConvWBXML2XMLParams
        {
            public UInt32 gen_type;
            public UInt32 lang;
            public byte indent;
            public byte keep_ignorable_ws;
        };

        public static string ConvertToXML(byte[] wbxml)
        {
            string xml;

            WBXMLConvWBXML2XMLParams parms = new WBXMLConvWBXML2XMLParams();
            parms.gen_type = WBXML_ENCODER_XML_GEN_CANONICAL;
            parms.lang = WBXML_LANG_AIRSYNC;
            parms.indent = 0;
            parms.keep_ignorable_ws = 0;

            UInt32 retval = wbxml_conv_wbxml2xml(wbxml, wbxml.Length, out xml, ref parms);
            if (retval == 0)
            {
                // skip the document type part, which is quite bogus anyway
                return xml.Substring(104);
            }
            else
            {
                throw new Exception(String.Format("Failed to parse wbxml: {0}", retval));
            }
        }
    }
}
