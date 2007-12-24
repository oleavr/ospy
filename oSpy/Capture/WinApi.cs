using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace oSpy.Capture
{
    public class WinApi
    {
        [DllImport ("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr OpenSCManager (string lpMachineName, string lpSCDB, int scParameter);

        [DllImport ("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateService (IntPtr SC_HANDLE, string lpSvcName, string lpDisplayName, int dwDesiredAccess, int dwServiceType, int dwStartType, int dwErrorControl, string lpPathName, string lpLoadOrderGroup, int lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport ("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void CloseServiceHandle (IntPtr SCHANDLE);

        [DllImport ("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool StartService (IntPtr SVHANDLE, int dwNumServiceArgs, string lpServiceArgVectors);

        [DllImport ("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ControlService (IntPtr hService, uint dwControl, ref SERVICE_STATUS lpServiceStatus);

        [DllImport ("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr OpenService (IntPtr SCHANDLE, string lpSvcName, int dwNumServiceArgs);

        [DllImport ("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int DeleteService (IntPtr SVHANDLE);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetupDiGetClassDevs (IntPtr classGuid,
            [MarshalAs (UnmanagedType.LPTStr)] string enumerator,
            IntPtr hwndParent,
            UInt32 flags);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo (IntPtr deviceInfoSet, uint memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList (IntPtr deviceInfoSet);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty (IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property, IntPtr propertyRegDataType, byte[] propertyBuffer, uint propertyBufferSize, out UInt32 requiredSize);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiSetDeviceRegistryProperty (IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property, byte[] propertyBuffer, uint propertyBufferSize);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiLoadClassIcon (ref Guid classGuid, out IntPtr largeIcon, out int miniIconIndex);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiLoadDeviceIcon (IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint cxIcon, uint cyIcon, uint flags, out IntPtr hIcon);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetClassImageList (ref SP_CLASSIMAGELIST_DATA classImageListData);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiGetClassImageIndex (ref SP_CLASSIMAGELIST_DATA classImageListData, ref Guid classGuid, out int imageIndex);

        [DllImport ("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetupDiDestroyClassImageList (ref SP_CLASSIMAGELIST_DATA classImageListData);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon (IntPtr handle);

        [DllImport ("comctl32.dll", SetLastError = true)]
        public static extern IntPtr ImageList_GetIcon (IntPtr himl, int i, int flags);

        [StructLayout (LayoutKind.Sequential)]
        public struct SERVICE_STATUS
        {
            public UInt32 dwServiceType;
            public UInt32 dwCurrentState;
            public UInt32 dwControlsAccepted;
            public UInt32 dwWin32ExitCode;
            public UInt32 dwServiceSpecificExitCode;
            public UInt32 dwCheckPoint;
            public UInt32 dwWaitHint;
        }

        [StructLayout (LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public UInt32 cbSize;
            public Guid ClassGuid;
            public UInt32 DevInst;
            public IntPtr Reserved;
        }

        [StructLayout (LayoutKind.Sequential)]
        public struct SP_CLASSIMAGELIST_DATA
        {
            public UInt32 cbSize;
            public IntPtr ImageList;
            public UInt32 Reserved;
        }

        public const int SC_MANAGER_ALL_ACCESS = 0xF003F;
        public const int SERVICE_ALL_ACCESS = 0xF01FF;

        public const int SERVICE_KERNEL_DRIVER = 0x00000001;

        public const int SERVICE_DEMAND_START = 0x00000003;

        public const int SERVICE_ERROR_NORMAL = 0x00000001;

        public const int SERVICE_CONTROL_STOP = 0x00000001;

        public const int ERROR_SERVICE_ALREADY_RUNNING = 1056;
        public const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;

        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_ALLCLASSES = 0x00000004;

        public const int SPDRP_DEVICEDESC = 0x00000000;
        public const int SPDRP_HARDWAREID = 0x00000001;
        public const int SPDRP_FRIENDLYNAME = 0x0000000C;
        public const int SPDRP_LOWERFILTERS = 0x00000012;
        public const int SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E;

        public const int INVALID_HANDLE_VALUE = -1;

        public static string ByteArrayToString (byte[] bytes)
        {
            int length = FindUnicodeCStringLength (bytes, 0);
            if (length == 0)
                return "";

            return Encoding.Unicode.GetString (bytes, 0, length);
        }

        public static string[] MarshalMultiStringToStringArray (byte[] bytes)
        {
            List<string> result = new List<string> ();

            int offset = 0;

            while (true)
            {
                int length = FindUnicodeCStringLength (bytes, offset);
                if (length == 0)
                    break;

                result.Add (Encoding.Unicode.GetString (bytes, offset, length));
                offset += length + 2;
            }

            return result.ToArray ();
        }

        private static int FindUnicodeCStringLength (byte[] bytes, int offset)
        {
            for (int i = offset; i + 1 < bytes.Length; i += 2)
            {
                if (bytes[i] == 0 && bytes[i + 1] == 0)
                    return i - offset;
            }

            throw new Exception ("NUL termination not found");
        }

        public static byte[] MarshalStringArrayToMultiString (string[] value)
        {
            List<byte> bytes = new List<byte> ();

            foreach (string s in value)
            {
                bytes.AddRange (Encoding.Unicode.GetBytes (s));

                bytes.Add (0);
                bytes.Add (0);
            }

            bytes.Add (0);
            bytes.Add (0);

            return bytes.ToArray ();
        }
    }
}
