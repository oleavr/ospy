using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace oSpy.Playground
{
    public class ColorPool
    {
        private List<Color> colors;
        private Dictionary<string, Color> usedColors;
        private int index;

        public ColorPool ()
        {
            Initialize (0);
        }

        public ColorPool (bool skipWhite)
        {
            Initialize (1);
        }

        public ColorPool (int index)
        {
            Initialize (index);
        }

        private void Initialize (int offset)
        {
            index = offset;

            colors = new List<Color> (32);
            usedColors = new Dictionary<string, Color> (32);

            //colors.Add(Color.FromName("#00001C"));
            //colors.Add(Color.FromName("#0B222D"));

            AddColor ("#FFFFFF");

            //AddColor("#522200");
            //AddColor("#B5002F");
            //AddColor("#F9BA07");
            //AddColor("#539316");
            AddColor ("#00662F");
            //AddColor("#004188");
            AddColor ("#1B2D83");
            AddColor ("#55127B");
            AddColor ("#7B0C82");
            AddColor ("#444E5A");

            AddColor ("#9F5F00");
            AddColor ("#E72300");
            AddColor ("#FFDC00");
            AddColor ("#63B01F");
            AddColor ("#009754");
            AddColor ("#0071BC");
            AddColor ("#4A6CB3");
            AddColor ("#7967AB");
            AddColor ("#9B579F");
            AddColor ("#6D7179");

            AddColor ("#D9AE7E");
            AddColor ("#F0A4B9");
            AddColor ("#FFE944");
            AddColor ("#7FBB56");
            AddColor ("#80C39B");
            AddColor ("#0094D5");
            AddColor ("#5F8FCB");
            AddColor ("#A49ECD");
            AddColor ("#BA90C0");
            AddColor ("#9DA1A6");
        }

        private void AddColor (string name)
        {
            Color c = Color.FromArgb (
                Convert.ToByte (name.Substring (1, 2), 16),
                Convert.ToByte (name.Substring (3, 2), 16),
                Convert.ToByte (name.Substring (5, 2), 16));

            colors.Add (c);
        }

        public Color GetColorForId (string id)
        {
            if (usedColors.ContainsKey (id))
                return usedColors[id];

            int wrapCount = index / colors.Count;
            int colIndex = index % colors.Count;

            Color baseColor = colors[colIndex];

            Color newColor = Color.FromArgb ((baseColor.R + (10 * wrapCount)) % 256,
                                            (baseColor.G + (10 * wrapCount)) % 256,
                                            (baseColor.B + (10 * wrapCount)) % 256);
            usedColors[id] = newColor;
            index++;

            return newColor;
        }
    }

    public class StaticUtils
    {
        public const int SB_HORZ = 0x0;
        public const int SB_VERT = 0x1;

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetScrollRange (IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetScrollPos (IntPtr hWnd, int nBar);

        public static string ToNormalizedAscii (byte[] bytes, int offset, int len)
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

        public static string ByteArrayToHexDump (byte[] bytes)
        {
            return ByteArrayToHexDump (bytes, -1);
        }

        public static string ByteArrayToHexDump (byte[] bytes, string prefix, out int lineCount)
        {
            return ByteArrayToHexDump (bytes, -1, prefix, out lineCount);
        }

        public static string ByteArrayToHexDump (byte[] bytes, int maxSize)
        {
            return ByteArrayToHexDump (bytes, maxSize, "");
        }

        public static string ByteArrayToHexDump (byte[] bytes, int maxSize, string prefix)
        {
            int lineCount;
            return ByteArrayToHexDump (bytes, maxSize, prefix, out lineCount);
        }

        public static string ByteArrayToHexDump (byte[] bytes, int maxSize, string prefix, out int lineCount)
        {
            return ByteArrayToHexDump (bytes, maxSize, prefix, out lineCount, 0);
        }

        public static string ByteArrayToHexDump (byte[] bytes, int maxSize, string prefix, out int lineCount, int remainingBytes)
        {
            StringBuilder dump = new StringBuilder (512);

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
                        dump.AppendFormat ("  {0}\n", ToNormalizedAscii (bytes, i - 16, 16));
                    }

                    dump.AppendFormat ("{0}{1:x4}:", prefix, i);
                }

                dump.AppendFormat (" {0:x2}", bytes[i]);
            }

            if (i != 0)
            {
                int remaining = (16 - (i % 16)) % 16;

                i--;

                lineCount++;
                string str = "{0, -" + Convert.ToString ((remaining * 3) + 2) + "}{1}\n";
                dump.AppendFormat (str, "", ToNormalizedAscii (bytes, i - (i % 16), (i % 16) + 1));
            }

            if (clamped)
                remainingBytes += bytes.Length - maxSize;

            if (remainingBytes > 0)
            {
                dump.AppendFormat ("{0}[{1} more bytes of data]", prefix, remainingBytes);
            }
            else
            {
                // Remove the trailing newline
                if (dump.Length > 0)
                    dump.Remove (dump.Length - 1, 1);
            }

            return dump.ToString ();
        }
    }
}
