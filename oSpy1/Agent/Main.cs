using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using EasyHook;

namespace oSpyAgent
{
    public class Main : IEntryPoint
    {
        public Main(RemoteHooking.IContext context, string binDir)
        {
        }

        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public void Run(RemoteHooking.IContext context, string binDir)
        {
            string legacyAgentPath = Path.Combine(binDir, "oSpyAgent32.dll");
            IntPtr handle = LoadLibrary(legacyAgentPath);
            if (handle != IntPtr.Zero)
                Thread.Sleep(Timeout.Infinite);
            else
                MessageBox.Show(String.Format("Failed to load agent (\"{0}\")", legacyAgentPath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
