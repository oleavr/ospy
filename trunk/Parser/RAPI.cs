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
using oSpy.Util;
using oSpy.Net;
using System.Collections.Generic;

namespace oSpy.Parser
{
    public class RAPITransactionFactory : TransactionFactory
    {
        private const int NOTIFY_INITIAL_HANDSHAKE = 0;
        private const int NOTIFY_CONNECTION_READY = 1;

        private const int MAX_DEVICE_INFO_LENGTH = 1024;

        private enum RAPIConnectionState
        {
            HANDSHAKE,
            AUTH,
            SESSION,
        };

        private static string[] rapiCallNames = new string[] {
            "RAPI_EXP_11", // 0x00
            "CeSyncTimeToPc", // 0x01 / RAPI_EXP_12
            "CeStartReplication", // 0x02 / RAPI_EXP_13
            "RAPI_EXP_14", // 0x03
            "RAPI_EXP_15", // 0x04
            "RAPI_EXP_16", // 0x05
            "RAPI_EXP_17", // 0x06
            "RAPI_EXP_18", // 0x07
            "RAPI_EXP_19", // 0x08
            "RAPI_EXP_20", // 0x09
            "RAPI_EXP_21", // 0x0a
            "RAPI_EXP_22", // 0x0b
            "RAPI_EXP_23", // 0x0c
            "RAPI_EXP_24", // 0x0d
            "CeProcessConfig", // 0x0e / RAPI_EXP_25
            "CeSyncPause", // 0x0f
            "CeSyncResume", // 0x10
            "CeFindFirstFile", // 0x11
            "CeFindNextFile", // 0x12
            "CeFindClose", // 0x13
            "CeGetFileAttributes", // 0x14
            "CeSetFileAttributes", // 0x15
            "CeCreateFile", // 0x16
            "CeReadFile", // 0x17
            "CeWriteFile", // 0x18
            "CeCloseHandle", // 0x19
            "CeFindAllFiles", // 0x1a
            "CeFindFirstDatabase", // 0x1b
            "CeFindNextDatabase", // 0x1c
            "CeOidGetInfo", // 0x1d
            "CeCreateDatabase", // 0x1e
            "CeOpenDatabase", // 0x1f
            "CeDeleteDatabase", // 0x20
            "CeReadRecordProps", // 0x21
            "CeWriteRecordProps", // 0x22
            "CeDeleteRecord", // 0x23
            "CeSeekDatabase", // 0x24
            "CeSetDatabaseInfo", // 0x25
            "CeSetFilePointer", // 0x26
            "CeSetEndOfFile", // 0x27
            "CeCreateDirectory", // 0x28
            "CeRemoveDirectory", // 0x29
            "CeCreateProcess", // 0x2a
            "CeMoveFile", // 0x2b
            "CeCopyFile", // 0x2c
            "CeDeleteFile", // 0x2d
            "CeGetFileSize", // 0x2e
            "CeRegOpenKeyEx", // 0x2f
            "CeRegEnumKeyEx", // 0x30
            "CeRegCreateKeyEx", // 0x31
            "CeRegCloseKey", // 0x32
            "CeRegDeleteKey", // 0x33
            "CeRegEnumValue", // 0x34
            "CeRegDeleteValue", // 0x35
            "CeRegQueryInfoKey", // 0x36
            "CeRegQueryValueEx", // 0x37
            "CeRegSetValueEx", // 0x38
            "CeGetStoreInformation", // 0x39
            "CeGetSystemMetrics", // 0x3a
            "CeGetDesktopDeviceCaps", // 0x3b
            "CeFindAllDatabases", // 0x3c
            "CeGetSystemInfo", // 0x3d
            "CeSHCreateShortcut", // 0x3e
            "CeSHGetShortcutTarget", // 0x3f
            "CeCheckPassword", // 0x40
            "CeGetFileTime", // 0x41
            "CeSetFileTime", // 0x42
            "CeGetVersionEx", // 0x43
            "CeGetWindow", // 0x44
            "CeGetWindowLong", // 0x45
            "CeGetWindowText", // 0x46
            "CeGetClassName", // 0x47
            "CeGlobalMemoryStatus", // 0x48
            "CeGetSystemPowerStatusEx", // 0x49
            "CeGetTempPath", // 0x4a
            "CeGetSpecialFolderPath", // 0x4b
            "CeRapiInvoke", // 0x4c
            "CeFindFirstDatabaseEx", // 0x4d
            "CeFindNextDatabaseEx", // 0x4e
            "CeCreateDatabaseEx", // 0x4f
            "CeSetDatabaseInfoEx", // 0x50
            "CeOpenDatabaseEx", // 0x51
            "CeDeleteDatabaseEx", // 0x52
            "CeReadRecordPropsEx", // 0x53
            "CeMountDBVol", // 0x54
            "CeUnmountDBVol", // 0x55
            "CeFlushDBVol", // 0x56
            "CeEnumDBVolumes", // 0x57
            "CeOidGetInfoEx", // 0x58
            "CeSyncStart", // 0x59
            "CeSyncStop", // 0x5a
            "CeQueryInstructionSet", // 0x5b
            "CeGetDiskFreeSpaceEx", // 0x5c
        };

        private IPSession session;
        private PacketStream stream;

        public RAPITransactionFactory(DebugLogger logger)
            : base(logger)
        {
        }

        public override string Name()
        {
            return "RAPI Transaction Factory";
        }

        public override bool HandleSession(IPSession session)
        {
            if (session.LocalEndpoint.Port != 990)
                return false;

            this.session = session;
            stream = session.GetNextStreamDirection();

            // The device should send the first DWORD of the handshake
            if (stream.CurPacket.Direction == PacketDirection.PACKET_DIRECTION_INCOMING)
            {
                HandleRapiHandshake();
            }

            HandleRapiSession();

            return true;
        }

        private void HandleRapiHandshake()
        {
            RAPIConnectionState state = RAPIConnectionState.HANDSHAKE;

            List<PacketSlice> slices = new List<PacketSlice>();
            TransactionNode parentNode, node;
            string str;
            UInt32 val;

            // Read and verify the initial request
            UInt32 initialRequest = stream.ReadU32LE(slices);
            if (initialRequest != NOTIFY_INITIAL_HANDSHAKE && initialRequest != NOTIFY_CONNECTION_READY)
            {
                logger.AddMessage("RAPI protocol error, unknown initial request {0}", initialRequest);
                return;
            }

            node = new TransactionNode((initialRequest == 0) ? "RAPIInitialHandshake" : "RAPIConnectionStart");
            node.Description = node.Name;

            node.AddField("InitialRequest", (initialRequest == NOTIFY_INITIAL_HANDSHAKE) ? "NOTIFY_INITIAL_HANDSHAKE" : "NOTIFY_CONNECTION_READY", "Initial request.", slices);

            // Now it's our turn
            stream = session.GetNextStreamDirection();

            if (initialRequest == NOTIFY_INITIAL_HANDSHAKE)
            {
                UInt32 firstPing = stream.ReadU32LE(slices);
                node.AddField("FirstPing", firstPing, "First ping, should be 3.", slices);

                // And the first pong
                stream = session.GetNextStreamDirection();

                UInt32 firstPong = stream.ReadU32LE(slices);
                node.AddField("FirstPong", firstPong, "First pong, should be 4 for older WM5, 6 for newer versions.", slices);

                if (firstPong == 6)
                {
                    // Now we're supposed to send 4 DWORDs
                    UInt32 secondPing = stream.ReadU32LE(slices);
                    node.AddField("SecondPingValue1", secondPing, "Second ping value #1, should be 7.", slices);

                    secondPing = stream.ReadU32LE(slices);
                    node.AddField("SecondPingValue2", secondPing, "Second ping value #2, should be 8.", slices);

                    secondPing = stream.ReadU32LE(slices);
                    node.AddField("SecondPingValue3", secondPing, "Second ping value #3, should be 4.", slices);

                    secondPing = stream.ReadU32LE(slices);
                    node.AddField("SecondPingValue4", secondPing, "Second ping value #4, should be 1.", slices);

                    // And the device should reply
                    stream = session.GetNextStreamDirection();

                    UInt32 secondPong = stream.ReadU32LE(slices);
                    node.AddField("SecondPong", secondPong, "Second pong, should be 4.", slices);
                }

                // Got it
                session.AddNode(node);

                parentNode = new TransactionNode("RAPIDeviceInfo");
                parentNode.Description = parentNode.Name;

                UInt32 deviceInfoLen = stream.ReadU32LE(slices);
                UInt32 remainingDevInfoLen = deviceInfoLen;
                parentNode.AddField("Length", deviceInfoLen, "Device info length.", slices);

                if (deviceInfoLen > MAX_DEVICE_INFO_LENGTH)
                {
                    logger.AddMessage("RAPI protocol error, length of the device info package should be below {0}, was {1}", MAX_DEVICE_INFO_LENGTH, deviceInfoLen);
                    return;
                }

                node = new TransactionNode(parentNode, "DeviceInfo");

                Guid guid = new Guid(stream.ReadBytes(16, slices));
                str = String.Format("{{0}}", guid.ToString());
                node.AddField("DeviceGUID", str, "Device GUID.", slices);
                remainingDevInfoLen -= 16;

                val = stream.ReadU32LE(slices);
                node.AddField("OsVersionMajor", val, "OS version, major.", slices);
                remainingDevInfoLen -= 4;

                val = stream.ReadU32LE(slices);
                node.AddField("OsVersionMinor", val, "OS version, minor.", slices);
                remainingDevInfoLen -= 4;

                val = stream.ReadU32LE(slices);
                node.AddField("DeviceNameLength", val, "Device name length (in characters, not bytes).", slices);
                remainingDevInfoLen -= 4;

                // calculate the string size in unicode, with terminating NUL word
                val = (val + 1) * 2;
                str = stream.ReadCStringUnicode((int)val, slices);
                node.AddField("DeviceName", str, "Device name.", slices);
                remainingDevInfoLen -= val;

                val = stream.ReadU32LE(slices);
                node.AddField("DeviceVersion", StaticUtils.FormatFlags(val), "Device version.", slices);
                remainingDevInfoLen -= 4;

                val = stream.ReadU32LE(slices);
                node.AddField("DeviceProcessorType", StaticUtils.FormatFlags(val), "Device processor type.", slices);
                remainingDevInfoLen -= 4;

                val = stream.ReadU32LE(slices);
                node.AddField("Unknown1", StaticUtils.FormatFlags(val), "Counter or a flag? ANDed with 0xFFFFFFFE in the code (should take a closer look at this).", slices);
                remainingDevInfoLen -= 4;

                val = stream.ReadU32LE(slices);
                node.AddField("CurrentPartnerId", StaticUtils.FormatFlags(val), "Current partner id.", slices);
                remainingDevInfoLen -= 4;

                val = stream.ReadU32LE(slices);
                node.AddField("DeviceId", StaticUtils.FormatFlags(val), "Current device id. Lives in HKCU\\Software\\Microsoft\\Windows CE Services\\Partners\\<DeviceIdentifier>.", slices);
                remainingDevInfoLen -= 4;

                /*
                dw = stream.ReadU32LE(slices);
                node.AddField("PlatformNameLength", dw, "Platform name length.", slices);
                remainingDevInfoLen -= 4;*/

                // Don't swallow the 4 last
                remainingDevInfoLen -= 4;

                byte[] bytes = stream.ReadBytes((int)remainingDevInfoLen, slices);
                node.AddField("UnknownData1", StaticUtils.FormatByteArray(bytes), "Unknown device info data.", slices);

                val = stream.ReadU32LE(slices);
                node.AddField("PasswordMask", StaticUtils.FormatFlags(val), "Password mask. Non-zero if a password is set.", slices);
                remainingDevInfoLen -= 4;

                state = (val != 0) ? RAPIConnectionState.AUTH : RAPIConnectionState.SESSION;

                // Now it's our turn
                stream = session.GetNextStreamDirection();

                node = parentNode;
            }
            else
            {
                state = RAPIConnectionState.SESSION;
            }

            // Add the last node for each case
            session.AddNode(node);

            while (state == RAPIConnectionState.AUTH)
            {
                parentNode = new TransactionNode("RAPIAuthAttempt");
                parentNode.Description = parentNode.Name;

                node = new TransactionNode(parentNode, "Request");

                val = stream.ReadU16LE(slices);
                node.AddField("Length", val, "Authentication data length.", slices);

                byte[] bytes = stream.ReadBytes((int)val, slices);
                node.AddField("Data", StaticUtils.FormatByteArray(bytes), "Authentication data.", slices);

                stream = session.GetNextStreamDirection();

                node = new TransactionNode(parentNode, "Response");

                val = stream.ReadU16LE(slices);
                node.AddField("Success", (val != 0) ? "TRUE" : "FALSE", "Whether the authentication attempt was successful.", slices);

                session.AddNode(parentNode);

                stream = session.GetNextStreamDirection();

                if (val != 0)
                    state = RAPIConnectionState.SESSION;
            }
        }

        private void HandleRapiSession()
        {
            List<PacketSlice> slices = new List<PacketSlice>();
            TransactionNode node;
            string str;
            UInt32 val, retVal, lastError;

            while (stream.GetBytesAvailable() > 0)
            {
                UInt32 msgLen, msgType;
                List<PacketSlice> msgLenSlices = new List<PacketSlice>(1);
                List<PacketSlice> msgTypeSlices = new List<PacketSlice>(1);

                logger.AddMessage("direction={0}", stream.CurPacket.Direction);

                // Message length
                msgLen = stream.ReadU32LE(msgLenSlices);
                logger.AddMessage("msgLen={0}", msgLen);

                if (msgLen == 5)
                {
                    node = new TransactionNode("RAPINotification");
                    node.Description = node.Name;

                    node.AddField("MessageType", "RAPI_NOTIFICATION", "Message type.", msgLenSlices);

                    val = stream.ReadU32LE(slices);
                    node.AddField("NotificationType", (val == 4) ? "REQUEST_NEW_CONNECTION" : StaticUtils.FormatFlags(val), "Notification type.", slices);

                    val = stream.ReadU32LE(slices);
                    node.AddField("Argument", val, "Argument.", slices);

                    session.AddNode(node);

                    if (stream.GetBytesAvailable() == 0)
                        break;
                    else
                        continue;
                }

                // Message type
                msgType = stream.ReadU32LE(msgTypeSlices);
                if (msgType >= rapiCallNames.Length)
                {
                    logger.AddMessage("Unknown call name: {0:x8}", msgType);
                    return;
                }

                string name = rapiCallNames[msgType];

                TransactionNode call = new TransactionNode(name);
                call.Description = call.Name;

                TransactionNode req = new TransactionNode(call, "Request");
                TransactionNode resp = new TransactionNode(call, "Response");

                req.AddField("MessageLength", msgLen, "Length of the RAPI request.", msgLenSlices);
                req.AddField("MessageType", String.Format("{0} (0x{1:x2})", name, msgType), "Type of the RAPI request.", msgTypeSlices);

                if (name == "CeRegOpenKeyEx")
                {
                    val = stream.ReadU32LE(slices);
                    req.AddField("hKey", StaticUtils.FormatRegKey(val),
                        "Handle to a currently open key or one of the following predefined reserved handle values:\n" +
                        "HKEY_CLASSES_ROOT\nHKEY_CURRENT_USER\nHKEY_LOCAL_MACHINE\nHKEY_USERS",
                        slices);

                    str = stream.ReadRAPIString(slices);
                    req.AddField("szSubKey", str,
                        "A null-terminated string containing the name of the subkey to open.",
                        slices);

                    req.Summary = String.Format("{0}, \"{1}\"", StaticUtils.FormatRegKey(val, true), str);

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError);

                    val = stream.ReadU32LE(slices);
                    string result = String.Format("0x{0:x8}", val);
                    resp.AddField("hkResult", result, "Handle to the opened key.", slices);

                    if (retVal == Constants.ERROR_SUCCESS)
                        resp.Summary = String.Format("{0}, {1}", StaticUtils.FormatRetVal(retVal, true), result);
                    else
                        resp.Summary = StaticUtils.FormatRetVal(retVal, true);
                }
                else if (name == "CeRegCreateKeyEx")
                {
                    val = stream.ReadU32LE(slices);
                    req.AddField("hKey", StaticUtils.FormatRegKey(val),
                        "Handle to a currently open key or one of the following predefined reserved handle values:\n" +
                        "HKEY_CLASSES_ROOT\nHKEY_CURRENT_USER\nHKEY_LOCAL_MACHINE\nHKEY_USERS", slices);

                    string szSubKey = stream.ReadRAPIString(slices);
                    req.AddField("szSubKey", (szSubKey != null) ? szSubKey : "(null)",
                        "A null-terminated string specifying the name of a subkey that this function opens or creates. " +
                        "The subkey specified must be a subkey of the key identified by the hKey parameter. This subkey " +
                        "must not begin with the backslash character (\\). If the parameter is NULL, then RegCreateKeyEx " +
                        "behaves like RegOpenKey, where it opens the key specified by hKey.", slices);

                    string szClass = stream.ReadRAPIString(slices);
                    req.AddField("szClass", (szClass != null) ? szClass : "(null)",
                        "A null-terminated string that specifies the class (object type) of this key. This parameter is " +
                        "ignored if the key already exists.", slices);

                    req.Summary = String.Format("{0}, {1}, {2}",
                        StaticUtils.FormatRegKey(val, true),
                        StaticUtils.FormatStringArgument(szSubKey),
                        StaticUtils.FormatStringArgument(szClass));

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError);

                    string result = String.Format("0x{0:x8}", stream.ReadU32LE(slices));
                    resp.AddField("hkResult", result, "Handle to the opened key.", slices);

                    UInt32 disposition = stream.ReadU32LE(slices);
                    resp.AddField("dwDisposition", StaticUtils.FormatRegDisposition(disposition),
                        "Receives one of REG_CREATED_NEW_KEY and REG_OPENED_EXISTING_KEY.",
                        slices);

                    if (retVal == Constants.ERROR_SUCCESS)
                        resp.Summary = String.Format("{0}, {1}, {2}",
                            StaticUtils.FormatRetVal(retVal, true), result, StaticUtils.FormatRegDisposition(disposition, true));
                    else
                        resp.Summary = StaticUtils.FormatRetVal(retVal, true);

                }
                else if (name == "CeRegCloseKey")
                {
                    val = stream.ReadU32LE(slices);
                    req.AddField("hKey", StaticUtils.FormatRegKey(val),
                        "Handle to the open key to close.", slices);

                    req.Summary = StaticUtils.FormatRegKey(val, true);

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError);

                    resp.Summary = StaticUtils.FormatRetVal(retVal, true);
                }
                else if (name == "CeRegQueryValueEx")
                {
                    val = stream.ReadU32LE(slices);
                    req.AddField("hKey", StaticUtils.FormatRegKey(val),
                        "Handle to a currently open key or any of the following predefined reserved handle values:\n" +
                        "HKEY_CLASSES_ROOT\nHKEY_CURRENT_USER\nHKEY_LOCAL_MACHINE\nHKEY_USERS", slices);

                    string szValueName = stream.ReadRAPIString(slices);
                    req.AddField("szValueName", szValueName,
                        "A string containing the name of the value to query.", slices);

                    UInt32 cbData = stream.ReadU32LE(slices);
                    req.AddField("cbData", cbData,
                        "A variable that specifies the maximum number of bytes to return.", slices);

                    req.Summary = String.Format("{0}, {1}, {2}",
                        StaticUtils.FormatRegKey(val, true),
                        StaticUtils.FormatStringArgument(szValueName),
                        cbData);

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError);

                    UInt32 dwType = stream.ReadU32LE(slices);
                    resp.AddField("dwType", StaticUtils.FormatRegType(dwType),
                        "The type of data associated with the specified value.",
                        slices);

                    cbData = stream.ReadU32LE(slices);
                    resp.AddField("cbData", Convert.ToString(cbData),
                        "The size of the data returned.", slices);

                    str = ReadAndFormatDataForRegType(dwType, cbData, slices);
                    if (str == null)
                        str = "NULL";
                    resp.AddField("Data", str, "The data returned.", slices);

                    resp.Summary = StaticUtils.FormatRetVal(retVal, true);
                }
                else if (name == "CeRegSetValueEx")
                {
                    UInt32 key = stream.ReadU32LE(slices);
                    req.AddField("hKey", StaticUtils.FormatRegKey(key),
                        "Handle to a currently open key or any of the following predefined reserved handle values:\n" +
                        "HKEY_CLASSES_ROOT\nHKEY_CURRENT_USER\nHKEY_LOCAL_MACHINE\nHKEY_USERS", slices);

                    string szValueName = stream.ReadRAPIString(slices);
                    req.AddField("szValueName", szValueName,
                        "String containing the name of the value to set. If a value with this name is not already " +
                        "present in the key, the function adds it to the key. If this parameter is NULL or an empty " +
                        "string, the function sets the type and data for the key's unnamed value. Registry keys do " +
                        "not have default values, but they can have one unnamed value, which can be of any type.",
                        slices);

                    UInt32 dwType = stream.ReadU32LE(slices);
                    req.AddField("dwType", StaticUtils.FormatRegType(dwType),
                        "Type of information to be stored as the value's data.",
                        slices);

                    UInt32 cbData = stream.ReadU32LE(slices);
                    req.AddField("cbData", Convert.ToString(cbData),
                        "Specifies the size, in bytes, of the information passed in the the Data field.",
                        slices);

                    str = ReadAndFormatDataForRegType(dwType, cbData, slices);
                    if (str == null)
                        str = "NULL";
                    req.AddField("Data", str, "Buffer containing the data to be stored with the specified value name.",
                                 slices);

                    string dataSummary;
                    if (dwType == Constants.REG_DWORD || dwType == Constants.REG_DWORD_BIG_ENDIAN)
                    {
                        dataSummary = str;
                    }
                    else
                        dataSummary = String.Format("[{0} bytes]", cbData);

                    req.Summary = String.Format("{0}, {1}, {2}, {3}",
                        StaticUtils.FormatRegKey(key), StaticUtils.FormatStringArgument(szValueName), StaticUtils.FormatRegType(dwType), dataSummary);

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError);

                    resp.Summary = StaticUtils.FormatRetVal(retVal, true);

                }
                else if (name == "CeProcessConfig")
                {
                    str = stream.ReadRAPIString(slices);
                    req.AddXMLField("szRequest", str, "Config request.", slices);

                    UInt32 flags = stream.ReadU32LE(slices);
                    req.AddField("dwFlags", StaticUtils.FormatFlags(flags), "Flags.", slices);

                    req.Summary = String.Format("[{0} bytes], 0x{1:x8}",
                        str.Length, flags);

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError);

                    str = stream.ReadRAPIString(slices);
                    resp.AddXMLField("szResponse", str, "Config response.", slices);

                    if (retVal == Constants.ERROR_SUCCESS)
                        resp.Summary = String.Format("{0}, [{1} bytes]",
                            StaticUtils.FormatRetVal(retVal, true), (str != null) ? str.Length : 0);
                    else
                        resp.Summary = StaticUtils.FormatRetVal(retVal, true);

                }
                else if (name == "CeGetDesktopDeviceCaps")
                {
                    string caps = FormatDeviceCaps(stream.ReadU32LE(slices));
                    req.AddField("nIndex", caps, "The item to return.", slices);

                    req.Summary = caps;

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError, false);

                    resp.Summary = StaticUtils.FormatValue(retVal);
                }
                else if (name == "CeSyncStart")
                {
                    string xml = stream.ReadRAPIString(slices);
                    req.AddXMLField("szXML", (xml != null) ? xml : "(null)",
                                    "Optional message.", slices);

                    if (xml != null)
                        req.Summary = String.Format("[{0} bytes]", xml.Length);
                    else
                        req.Summary = "NULL";

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError);

                    resp.Summary = StaticUtils.FormatRetVal(retVal, true);
                }
                else if (name == "CeSyncResume" || name == "CeSyncPause")
                {
                    req.Summary = "";

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError, false);

                    resp.Summary = StaticUtils.FormatRetVal(retVal, true);
                }
                else if (name == "CeStartReplication")
                {
                    req.Summary = "";

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError, false);

                    resp.Summary = StaticUtils.FormatBool(retVal);
                }
                else if (name == "CeGetFileAttributes")
                {
                    string fileName = stream.ReadRAPIString(slices);

                    req.AddXMLField("szFileName", fileName,
                        "Name of a file or directory.", slices);

                    req.Summary = String.Format("\"{0}\"", fileName);

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError, false);

                    resp.Summary = StaticUtils.FormatValue(retVal);
                }
                else
                {
                    if (msgLen > 4)
                    {
                        byte[] bytes = stream.ReadBytes((int)msgLen - 4, slices);
                        req.AddField("UnparsedData", StaticUtils.FormatByteArray(bytes), "Unparsed data.", slices);
                    }

                    req.Summary = "[not yet parsed]";

                    SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError, false);

                    resp.Summary = "[not yet parsed]";
                }

                session.AddNode(call);

                stream = session.GetNextStreamDirection();
                if (stream.GetBytesAvailable() == 0)
                    break;
            }
        }

        private void SwitchToResponseAndParseResult(TransactionNode resp, List<PacketSlice> slices,
                                                    out UInt32 retVal, out UInt32 lastError)
        {
            SwitchToResponseAndParseResult(resp, slices, out retVal, out lastError, true);
        }

        private void SwitchToResponseAndParseResult(TransactionNode resp, List<PacketSlice> slices,
                                                    out UInt32 retVal, out UInt32 lastError,
                                                    bool formatRetVal)
        {
            stream = session.GetNextStreamDirection();

            UInt32 msgLen = stream.ReadU32LE(slices);
            resp.AddField("MessageLength", Convert.ToString(msgLen),
                "Length of the RAPI response.", slices);

            lastError = stream.ReadU32LE(slices);
            resp.AddField("LastError", String.Format("0x{0:x8}", lastError),
                "Last error on the CE device.", slices);

            retVal = stream.ReadU32LE(slices);
            resp.AddField("ReturnValue", (formatRetVal) ? StaticUtils.FormatRetVal(retVal) : StaticUtils.FormatValue(retVal),
                "Return value.", slices);
        }

        protected string ReadAndFormatDataForRegType(UInt32 dataType, UInt32 dataLength, List<PacketSlice> slices)
        {
            string str;

            if (dataLength == 0)
                return null;

            switch (dataType)
            {
                case Constants.REG_DWORD:
                    str = String.Format("0x{0:x8}", stream.ReadU32LE(slices));
                    break;
                case Constants.REG_SZ:
                    str = stream.ReadCStringUnicode((int)dataLength, slices);
                    break;
                default:
                    byte[] bytes = stream.ReadBytes((int)dataLength, slices);
                    str = StaticUtils.FormatByteArray(bytes);
                    break;
            }

            return str;
        }

        protected const int DRIVERVERSION = 0;
        protected const int TECHNOLOGY = 2;
        protected const int HORZSIZE = 4;
        protected const int VERTSIZE = 6;
        protected const int HORZRES = 8;
        protected const int VERTRES = 10;
        protected const int BITSPIXEL = 12;
        protected const int PLANES = 14;
        protected const int NUMBRUSHES = 16;
        protected const int NUMPENS = 18;
        protected const int NUMMARKERS = 20;
        protected const int NUMFONTS = 22;
        protected const int NUMCOLORS = 24;
        protected const int PDEVICESIZE = 26;
        protected const int CURVECAPS = 28;
        protected const int LINECAPS = 30;
        protected const int POLYGONALCAPS = 32;
        protected const int TEXTCAPS = 34;
        protected const int CLIPCAPS = 36;
        protected const int RASTERCAPS = 38;
        protected const int ASPECTX = 40;
        protected const int ASPECTY = 42;
        protected const int ASPECTXY = 44;
        protected const int PHYSICALWIDTH = 110;
        protected const int PHYSICALHEIGHT = 111;
        protected const int PHYSICALOFFSETX = 112;
        protected const int PHYSICALOFFSETY = 113;
        protected const int SHADEBLENDCAPS = 120;

        protected string FormatDeviceCaps(UInt32 caps)
        {
            // FIXME: write a python conversion utility that autogenerates
            //        code that initializes a hashtable and use that ...
            switch (caps)
            {
                case DRIVERVERSION: return "DRIVERVERSION";
                case TECHNOLOGY: return "TECHNOLOGY";
                case HORZSIZE: return "HORZSIZE";
                case VERTSIZE: return "VERTSIZE";
                case HORZRES: return "HORZRES";
                case VERTRES: return "VERTRES";
                case BITSPIXEL: return "BITSPIXEL";
                case PLANES: return "PLANES";
                case NUMBRUSHES: return "NUMBRUSHES";
                case NUMPENS: return "NUMPENS";
                case NUMMARKERS: return "NUMMARKERS";
                case NUMFONTS: return "NUMFONTS";
                case NUMCOLORS: return "NUMCOLORS";
                case PDEVICESIZE: return "PDEVICESIZE";
                case CURVECAPS: return "CURVECAPS";
                case LINECAPS: return "LINECAPS";
                case POLYGONALCAPS: return "POLYGONALCAPS";
                case TEXTCAPS: return "TEXTCAPS";
                case CLIPCAPS: return "CLIPCAPS";
                case RASTERCAPS: return "RASTERCAPS";
                case ASPECTX: return "ASPECTX";
                case ASPECTY: return "ASPECTY";
                case ASPECTXY: return "ASPECTXY";
                case PHYSICALWIDTH: return "PHYSICALWIDTH";
                case PHYSICALHEIGHT: return "PHYSICALHEIGHT";
                case PHYSICALOFFSETX: return "PHYSICALOFFSETX";
                case PHYSICALOFFSETY: return "PHYSICALOFFSETY";
                case SHADEBLENDCAPS: return "SHADEBLENDCAPS";
                default:
                    return String.Format("0x{0:x8}", caps);
            }
        }
    }
}

#if false
    class RAPICallNodeConverter : ExpandableObjectConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(RAPICallNode))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) &&
                value is RAPICallNode)
            {
                RAPICallNode call = value as RAPICallNode;

                return String.Format("{0}({1}) => {2}", call.Name,
                    call.Request.Summary, call.Response.Summary);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

#endif
