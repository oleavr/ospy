//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace oSpy.Util
{
    public class StaticUtils
    {
        protected static Decoder asciiDecoder;
        protected static Encoder utf8Encoder;
        protected static Decoder utf8Decoder;

        static StaticUtils()
        {
            asciiDecoder = Encoding.ASCII.GetDecoder();

            utf8Encoder = Encoding.UTF8.GetEncoder();
            utf8Decoder = Encoding.UTF8.GetDecoder();
        }

        public const int SB_HORZ = 0x0;
        public const int SB_VERT = 0x1;
        public const int SB_THUMBPOSITION = 4;

        public const int WM_HSCROLL = 0x114;
        public const int WM_VSCROLL = 0x115;

        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("User32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("user32.dll")]
        public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        public static string ToNormalizedAscii(byte[] bytes, int offset, int len)
        {
            string s = "";

            for (int i = 0; i < len; i++)
            {
                byte b = bytes[offset + i];
                char c;

                if (b >= 33 && b <= 126)
                {
                    c = (char) b;
                }
                else
                {
                    c = '.';
                }

                s += c;
            }

            return s;
        }

        public static string ByteArrayToHexDump(byte[] bytes)
        {
            return ByteArrayToHexDump(bytes, -1);
        }

        public static string ByteArrayToHexDump(byte[] bytes, string prefix, out int lineCount)
        {
            return ByteArrayToHexDump(bytes, -1, prefix, out lineCount);
        }

        public static string ByteArrayToHexDump(byte[] bytes, int maxSize)
        {
            return ByteArrayToHexDump(bytes, maxSize, "");
        }

        public static string ByteArrayToHexDump(byte[] bytes, int maxSize, string prefix)
        {
            int lineCount;
            return ByteArrayToHexDump(bytes, maxSize, prefix, out lineCount);
        }

        public static string ByteArrayToHexDump(byte[] bytes, int maxSize, string prefix, out int lineCount)
        {
            return ByteArrayToHexDump(bytes, maxSize, prefix, out lineCount, 0);
        }

        public static string ByteArrayToHexDump(byte[] bytes, int maxSize, string prefix, out int lineCount, int remainingBytes)
        {
            StringBuilder dump = new StringBuilder(512);

            lineCount = 0;

            int len = bytes.Length;
            bool clamped = false;

            if (maxSize >= 0)
            {
                if (len > maxSize)
                {
                    len = maxSize;
                    clamped = true;
                }
            }

            int i;
            for (i = 0; i < len; i++)
            {
                if (i % 16 == 0)
                {
                    if (i != 0)
                    {
                        lineCount++;
                        dump.AppendFormat("  {0}\n", ToNormalizedAscii(bytes, i - 16, 16));
                    }

                    dump.AppendFormat("{0}{1:x4}:", prefix, i);
                }

                dump.AppendFormat(" {0:x2}", bytes[i]);
            }

            if (i != 0)
            {
                int remaining = (16 - (i % 16)) % 16;

                i--;

                lineCount++;
                string str = "{0, -" + Convert.ToString((remaining * 3) + 2) + "}{1}\n";
                dump.AppendFormat(str, "", ToNormalizedAscii(bytes, i - (i % 16), (i % 16) + 1));
            }

            if (clamped)
                remainingBytes += bytes.Length - maxSize;

            if (remainingBytes > 0)
            {
                dump.AppendFormat("{0}[{1} more bytes of data]", prefix, remainingBytes);
            }
            else
            {
                // Remove the trailing newline
                if (dump.Length > 0)
                    dump.Remove(dump.Length - 1, 1);
            }

            return dump.ToString();
        }

        public static string FormatByteArray(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder((3 * bytes.Length) - 1);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0)
                    builder.Append(" ");
                builder.AppendFormat("{0:x2}", bytes[i]);
            }
            return builder.ToString();
        }

        public static string FormatRetVal(UInt32 retVal)
        {
            return FormatRetVal(retVal, false);
        }

        public static string FormatRetVal(UInt32 retVal, bool compressed)
        {
            switch (retVal)
            {
                case 0:
                    if (!compressed)
                        return "ERROR_SUCCESS";
                    else
                        return "SUCCESS";
                case 2:
                    if (!compressed)
                        return "ERROR_FILE_NOT_FOUND";
                    else
                        return "FILE_NOT_FOUND";
                default:
                    return String.Format("0x{0:x8}", retVal);
            }
        }

        public static string FormatRegKey(UInt32 rKey)
        {
            return FormatRegKey(rKey, false);
        }

        public static string FormatRegKey(UInt32 rKey, bool compressed)
        {
            string str = "";

            RegistryHive hive = (RegistryHive)rKey;
            switch (hive)
            {
                case RegistryHive.ClassesRoot:
                    if (!compressed)
                        str = "HKEY_CLASSES_ROOT";
                    else
                        str = "HKCR";
                    break;
                case RegistryHive.CurrentUser:
                    if (!compressed)
                        str = "HKEY_CURRENT_USER";
                    else
                        str = "HKCU";
                    break;
                case RegistryHive.LocalMachine:
                    if (!compressed)
                        str = "HKEY_LOCAL_MACHINE";
                    else
                        str = "HKLM";
                    break;
                case RegistryHive.Users:
                    if (!compressed)
                        str = "HKEY_USERS";
                    else
                        str = "HKU";
                    break;
                default:
                    str = String.Format("0x{0:x8}", rKey);
                    break;
            }

            return str;
        }

        protected static string[] RegTypeNames = new string[]
        {
            "REG_NONE",                 // 0
            "REG_SZ",                   // 1
            "REG_EXPAND_SZ",            // 2
            "REG_BINARY",               // 3
            "REG_DWORD",                // 4
            "REG_DWORD_BIG_ENDIAN",     // 5
            "REG_LINK",                 // 6
            "REG_MULTI_SZ",             // 7
            "REG_RESOURCE_LIST",        // 8
        };

        public static string FormatRegType(UInt32 regType)
        {
            if (regType < RegTypeNames.Length)
                return RegTypeNames[regType];
            else
                return String.Format("0x{0:x8}", regType);
        }

        public static string FormatStringArgument(string arg)
        {
            if (arg != null)
                return String.Format("\"{0}\"", arg);
            else
                return "NULL";
        }

        public static string FormatFlags(uint flags)
        {
            if (flags != 0)
                return String.Format("0x{0:x8}", flags);
            else
                return "0";
        }

        public static string FormatValue(uint value)
        {
            if (value != 0)
                return String.Format("{0} (0x{1:x8})", value, value);
            else
                return "0";
        }

        public static string FormatRegDisposition(UInt32 disposition)
        {
            return FormatRegDisposition(disposition, false);
        }

        public static string FormatRegDisposition(UInt32 disposition, bool compressed)
        {
            switch (disposition)
            {
                case Constants.REG_CREATED_NEW_KEY:
                    if (!compressed)
                        return "REG_CREATED_NEW_KEY";
                    else
                        return "CREATED_NEW";
                case Constants.REG_OPENED_EXISTING_KEY:
                    if (!compressed)
                        return "REG_OPENED_EXISTING_KEY";
                    else
                        return "OPENED_EXISTING";
                default:
                    return String.Format("0x{0:x8}", disposition);
            }
        }

        public static string FormatBool(UInt32 retVal)
        {
            return (retVal != 0) ? "TRUE" : "FALSE";
        }

        public static string DecodeASCII(byte[] bytes)
        {
            string s = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                char c;

                if ((b >= 32 && b <= 126) || (b >= 9 && b <= 11) || b == 13)
                {
                    c = (char)b;
                }
                else
                {
                    c = '.';
                }

                s += c;
            }

            return s;
        }

        public static string DecodeUTF8(byte[] bytes)
        {
            int charCount = utf8Decoder.GetCharCount(bytes, 0, bytes.Length, true);
            char[] chars = new char[charCount];
            utf8Decoder.GetChars(bytes, 0, bytes.Length, chars, 0);
            return new string(chars);
        }

        public static int GetUTF8ByteCount(string s)
        {
            char[] chars = s.ToCharArray();
            return utf8Encoder.GetByteCount(chars, 0, chars.Length, true);
        }

        public static byte[] EncodeUTF8(string s)
        {
            char[] chars = s.ToCharArray();

            int byteCount = utf8Encoder.GetByteCount(chars, 0, chars.Length, true);

            byte[] bytes = new byte[byteCount];
            utf8Encoder.GetBytes(chars, 0, chars.Length, bytes, 0, true);

            return bytes;
        }
    }

    public class IDA
    {
        private delegate bool EnumWindowsHandler(int hWnd, int lParam);

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_RESTORE = 0xF120;

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsHandler lpEnumFunc, int lParam);
        [DllImport("user32.dll")]
        private static extern int RealGetWindowClass(int hwnd, StringBuilder pszType, int cchType);
        [DllImport("user32.dll")]
        private static extern int GetWindowText(int hWnd, StringBuilder s, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(int hWnd);
        [DllImport("user32.dll")]
        private static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.DLL")]
        private static extern bool SetForegroundWindow(int hWnd);

        private static int idaHWnd;
        private static string idaCallerModName;

        public static void GoToAddressInIDA(string moduleName, UInt32 address)
        {
            idaCallerModName = moduleName;
            idaHWnd = -1;

            EnumWindows(FindIDAWindowCallback, 0);
            if (idaHWnd == -1)
            {
                MessageBox.Show(String.Format("No IDA window with {0} found.", idaCallerModName),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (IsIconic(idaHWnd))
            {
                SendMessage(idaHWnd, WM_SYSCOMMAND, SC_RESTORE, 0);
            }
            else
            {
                SetForegroundWindow(idaHWnd);
            }

            System.Threading.Thread.Sleep(100);

            SendKeys.SendWait("g");
            SendKeys.SendWait(String.Format("{0:x}", address));
            SendKeys.SendWait("{ENTER}");
        }

        private static bool FindIDAWindowCallback(int hWnd, int lParam)
        {
            StringBuilder str = new StringBuilder(256);

            RealGetWindowClass(hWnd, str, str.Capacity);
            string cls = str.ToString();
            if (cls != "TApplication")
                return true;

            GetWindowText(hWnd, str, str.Capacity);
            string title = str.ToString();
            if (!title.StartsWith("IDA - "))
                return true;

            Match match = Regex.Match(title, @"^IDA - (?<path>.*?)( \((?<srcfile>.*?)\))?$");
            if (!match.Success)
                return true;

            string path = match.Groups["path"].Value;
            string srcfile = match.Groups["srcfile"].Value;
            if (srcfile == "")
            {
                srcfile = System.IO.Path.GetFileName(path);
            }

            if (string.Compare(srcfile, idaCallerModName, true) == 0)
            {
                idaHWnd = hWnd;
                return false;
            }

            return true;
        }
    }

    public class ColorPool
    {
        private List<Color> colors;
        private Dictionary<string, Color> usedColors;
        private int index;

        public ColorPool()
        {
            Initialize(0);
        }

        public ColorPool(bool skipWhite)
        {
            Initialize(1);
        }

        public ColorPool(int index)
        {
            Initialize(index);
        }

        private void Initialize(int offset)
        {
            index = offset;

            colors = new List<Color>(32);
            usedColors = new Dictionary<string, Color>(32);

            //colors.Add(Color.FromName("#00001C"));
            //colors.Add(Color.FromName("#0B222D"));

            AddColor("#FFFFFF");

            //AddColor("#522200");
            //AddColor("#B5002F");
            //AddColor("#F9BA07");
            //AddColor("#539316");
            AddColor("#00662F");
            //AddColor("#004188");
            AddColor("#1B2D83");
            AddColor("#55127B");
            AddColor("#7B0C82");
            AddColor("#444E5A");

            AddColor("#9F5F00");
            AddColor("#E72300");
            AddColor("#FFDC00");
            AddColor("#63B01F");
            AddColor("#009754");
            AddColor("#0071BC");
            AddColor("#4A6CB3");
            AddColor("#7967AB");
            AddColor("#9B579F");
            AddColor("#6D7179");

            AddColor("#D9AE7E");
            AddColor("#F0A4B9");
            AddColor("#FFE944");
            AddColor("#7FBB56");
            AddColor("#80C39B");
            AddColor("#0094D5");
            AddColor("#5F8FCB");
            AddColor("#A49ECD");
            AddColor("#BA90C0");
            AddColor("#9DA1A6");
        }

        private void AddColor(string name)
        {
            Color c = Color.FromArgb(
                Convert.ToByte(name.Substring(1, 2), 16),
                Convert.ToByte(name.Substring(3, 2), 16),
                Convert.ToByte(name.Substring(5, 2), 16));

            colors.Add(c);
        }

        public Color GetColorForId(string id)
        {
            if (usedColors.ContainsKey(id))
                return usedColors[id];

            int wrapCount = index / colors.Count;
            int colIndex = index % colors.Count;

            Color baseColor = colors[colIndex];

            Color newColor = Color.FromArgb((baseColor.R + (10 * wrapCount)) % 256,
                                            (baseColor.G + (10 * wrapCount)) % 256,
                                            (baseColor.B + (10 * wrapCount)) % 256);
            usedColors[id] = newColor;
            index++;

            return newColor;
        }
    }
}
