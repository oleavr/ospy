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
using System.Drawing;
using System.Runtime.InteropServices;

namespace oSpy.Capture
{
    public class DeviceEnumerator
    {
        private string id;
        public string Id
        {
            get { return id; }
        }

        public DeviceEnumerator (string id)
        {
            this.id = id;
        }

        public static DeviceEnumerator USB = new DeviceEnumerator ("USB");
    }

    public class DeviceList
    {
        private IntPtr devInfo;

        private List<Device> devices = new List<Device> ();
        public List<Device> Devices
        {
            get { return devices; }
        }

        public DeviceList (DeviceEnumerator enumerator)
        {
            List<string> presentIds = new List<string> ();

            devInfo = WinApi.SetupDiGetClassDevs (IntPtr.Zero, enumerator.Id, IntPtr.Zero, WinApi.DIGCF_ALLCLASSES | WinApi.DIGCF_PRESENT);
            if (devInfo.ToInt32 () != WinApi.INVALID_HANDLE_VALUE)
            {
                try
                {
                    foreach (Device device in EnumerateDevices (devInfo))
                        presentIds.Add (device.HardwareId);
                }
                finally
                {
                    WinApi.SetupDiDestroyDeviceInfoList (devInfo);
                }
            }

            devInfo = WinApi.SetupDiGetClassDevs (IntPtr.Zero, enumerator.Id, IntPtr.Zero, WinApi.DIGCF_ALLCLASSES);
            if (devInfo.ToInt32 () != WinApi.INVALID_HANDLE_VALUE)
            {
                devices = EnumerateDevices (devInfo);

                foreach (Device device in devices)
                    device.Present = presentIds.Contains (device.HardwareId);
            }

            devices.Sort ();
        }

        // TODO: destroy devInfo when disposing

        private static List<Device> EnumerateDevices (IntPtr devInfo)
        {
            List<Device> devices = new List<Device> ();

            for (uint i = 0; ; i++)
            {
                WinApi.SP_DEVINFO_DATA devInfoData = new WinApi.SP_DEVINFO_DATA ();
                devInfoData.cbSize = (uint)Marshal.SizeOf (devInfoData);

                if (!WinApi.SetupDiEnumDeviceInfo (devInfo, i, ref devInfoData))
                    break;

                Device device = Device.FromDevInfo (devInfo, devInfoData);
                if (device != null)
                    devices.Add (device);
            }

            return devices;
        }
    }

    public class Device : IComparable<Device>
    {
        private IntPtr devInfo;
        private WinApi.SP_DEVINFO_DATA devInfoData;

        private string name;
	public string Name
	{
	  get { return name; }
	}

        private string hardwareId;
        public string HardwareId
        {
            get { return hardwareId; }
        }

        private bool present = false;
        public bool Present
        {
            get { return present; }
            set { present = value; }
        }

        private static bool imageListDataLoaded = false;
        private static WinApi.SP_CLASSIMAGELIST_DATA imageListData;
        private static WinApi.SP_CLASSIMAGELIST_DATA ImageListData
        {
            get
            {
                if (!imageListDataLoaded)
                {
                    imageListDataLoaded = true;
                    imageListData = new WinApi.SP_CLASSIMAGELIST_DATA ();
                    imageListData.cbSize = (uint)Marshal.SizeOf (imageListData);
                    WinApi.SetupDiGetClassImageList (ref imageListData);
                }

                return imageListData;
            }
        }

        // TODO: WinApi.SetupDiDestroyClassImageList (ref imgListData);

        private Icon smallIcon = null;
        public Icon SmallIcon
        {
            get
            {
                EnsureIcons ();
                return smallIcon;
            }
        }

        private Icon largeIcon = null;
        public Icon LargeIcon
        {
            get
            {
                EnsureIcons ();
                return largeIcon;
            }
        }

        public string[] LowerFilters
        {
            get { return GetRegistryPropertyMultiString (WinApi.SPDRP_LOWERFILTERS); }
        }

        private Device (IntPtr devInfo, WinApi.SP_DEVINFO_DATA devInfoData, string name, string hardwareId)
        {
            this.devInfo = devInfo;
            this.devInfoData = devInfoData;
            this.name = name;
            this.hardwareId = hardwareId;
        }

        private bool iconsLoaded = false;

        private void EnsureIcons ()
        {
            if (iconsLoaded)
                return;

            iconsLoaded = true;

            LoadIcons (out smallIcon, out largeIcon);
        }

        private bool LoadIcons (out Icon smallIcon, out Icon largeIcon)
        {
            smallIcon = null;
            largeIcon = null;

            IntPtr smallIconHandle = IntPtr.Zero;
            IntPtr largeIconHandle = IntPtr.Zero;

            try
            {
                System.OperatingSystem osInfo = System.Environment.OSVersion;
                Version osVer = osInfo.Version;

                // Are we on Vista or newer?
                if (osInfo.Platform == PlatformID.Win32NT && osVer >= new Version (6, 0))
                {
                    if (!WinApi.SetupDiLoadDeviceIcon (devInfo, ref devInfoData, 16, 16, 0, out smallIconHandle))
                        return false;
                }
                else
                {
                    // Get the small icon's index
                    int smallIconIndex;
                    WinApi.SP_CLASSIMAGELIST_DATA clsImageListData = ImageListData;
                    WinApi.SetupDiGetClassImageIndex (ref clsImageListData, ref devInfoData.ClassGuid, out smallIconIndex);

                    // Retrieve the small icon
                    smallIconHandle = WinApi.ImageList_GetIcon (ImageListData.ImageList, smallIconIndex, 1);
                    if (smallIconHandle == IntPtr.Zero)
                        return false;
                }

                // Get the large icon
                int miniIconIndex;
                if (!WinApi.SetupDiLoadClassIcon (ref devInfoData.ClassGuid, out largeIconHandle, out miniIconIndex))
                    return false;

                // Create the icons in the managed world
                try
                {
                    smallIcon = Icon.FromHandle (smallIconHandle);
                    smallIconHandle = IntPtr.Zero;
                    largeIcon = Icon.FromHandle (largeIconHandle);
                    largeIconHandle = IntPtr.Zero;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            finally
            {
                if (smallIconHandle != IntPtr.Zero)
                    WinApi.DestroyIcon (smallIconHandle);

                if (largeIconHandle != IntPtr.Zero)
                    WinApi.DestroyIcon (largeIconHandle);
            }

            return true;
        }

        private string[] GetRegistryPropertyMultiString (uint property)
        {
            string[] value = new string [0];

            byte[] buf = new byte[1024];
            uint reqBufSize;
            if (WinApi.SetupDiGetDeviceRegistryProperty (devInfo, ref devInfoData, property, IntPtr.Zero, buf, (uint) buf.Length, out reqBufSize))
                value = WinApi.MarshalMultiStringToStringArray (buf);

            return value;
        }

        private void SetRegistryPropertyMultiString (uint property, string[] value)
        {
            byte[] buf = WinApi.MarshalStringArrayToMultiString (value);
            if (!WinApi.SetupDiSetDeviceRegistryProperty (devInfo, ref devInfoData, property, buf, (uint)buf.Length))
                throw new Exception (String.Format ("SetupDiSetDeviceRegistryProperty failed with error 0x{0:x8}", Marshal.GetLastWin32Error ()));
        }

        public bool HasLowerFilter (string name)
        {
            return Array.IndexOf<string> (LowerFilters, name) >= 0;
        }

        public void AddLowerFilter (string name)
        {
            List<string> filters = new List<string> (LowerFilters);
            if (filters.Contains (name))
                return;

            filters.Add (name);

            SetRegistryPropertyMultiString (WinApi.SPDRP_LOWERFILTERS, filters.ToArray ());
        }

        public int CompareTo (Device other)
        {
            if (other == this)
                return 0;
            else if (other.present != this.present)
                return other.present.CompareTo (this.present);
            else
                return this.name.CompareTo (other.name);
        }

        public static Device FromDevInfo (IntPtr devInfo, WinApi.SP_DEVINFO_DATA devInfoData)
        {
            Device device = null;

            string name = null;
            string hardwareId = null;

            byte[] buf = new byte[1024];
            uint reqBufSize;

            if (WinApi.SetupDiGetDeviceRegistryProperty (devInfo, ref devInfoData, WinApi.SPDRP_FRIENDLYNAME, IntPtr.Zero, buf, (uint) buf.Length, out reqBufSize))
            {
                name = WinApi.ByteArrayToString (buf);
            }

            if (name == null)
            {
                if (WinApi.SetupDiGetDeviceRegistryProperty (devInfo, ref devInfoData, WinApi.SPDRP_DEVICEDESC, IntPtr.Zero, buf, (uint)buf.Length, out reqBufSize))
                {
                    name = WinApi.ByteArrayToString (buf);
                }
            }

            if (WinApi.SetupDiGetDeviceRegistryProperty (devInfo, ref devInfoData, WinApi.SPDRP_HARDWAREID, IntPtr.Zero, buf, (uint)buf.Length, out reqBufSize))
            {
                string[] tokens = WinApi.MarshalMultiStringToStringArray (buf);
                hardwareId = String.Join (";", tokens);
            }

            if (name != null && hardwareId != null)
            {
                device = new Device (devInfo, devInfoData, name, hardwareId);
            }

            return device;
        }
    }
}
