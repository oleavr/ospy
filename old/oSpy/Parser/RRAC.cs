//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

// FIXME: port to new API
#if false

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace oSpy
{
    public class RRACTransactionFactory : StreamTransactionFactory
    {
        public const int RRAC_PORT = 5678;

        public RRACTransactionFactory(DebugLogger logger)
            : base(new int[] { RRAC_PORT }, null, logger)
        {
        }

        protected override StreamSession CreateSession(Packet firstPacket)
        {
            RRACSession.SubProtocol proto;

            if (sessions.Count == 0)
                proto = RRACSession.SubProtocol.Control;
            else
                proto = RRACSession.SubProtocol.Data;

            return new RRACSession(logger, proto);
        }
    }

    public class RRACSession : StreamSession
    {
        public enum SubProtocol
        {
            Control,
            Data,
        };

        private SubProtocol subProtocol;

        private enum RRACCommand
        {
            GetMetaData,
            SetMetaData,
            Notify,
            Response,
            ChangeLog,
            DeleteObject,
            GetObject,
            Ack,
            Nack,
            UNKNOWN
        };

        private static Dictionary<UInt16, RRACCommand> knownCommands;

        private Dictionary<RRACCommand, TransactionNode> pendingReplies;

        static RRACSession()
        {
            knownCommands = new Dictionary<ushort, RRACCommand>();

            knownCommands[0x6f] = RRACCommand.GetMetaData;
            knownCommands[0x70] = RRACCommand.SetMetaData;
            knownCommands[0x69] = RRACCommand.Notify;
            knownCommands[0x6c] = RRACCommand.Response;
            knownCommands[0x69] = RRACCommand.ChangeLog;
            knownCommands[0x66] = RRACCommand.DeleteObject;
            knownCommands[0x67] = RRACCommand.GetObject;
            knownCommands[0x65] = RRACCommand.Ack;
            knownCommands[0x6E] = RRACCommand.Nack;
        }

        public RRACSession(DebugLogger logger, SubProtocol subProtocol)
            : base(logger)
        {
            this.subProtocol = subProtocol;
            this.pendingReplies = new Dictionary<RRACCommand, TransactionNode>(1);
        }

        public override void HandlePacket(Packet packet)
        {
            switch (subProtocol)
            {
                case SubProtocol.Control:
                    HandleControlPacket(packet);
                    break;
                case SubProtocol.Data:
                    HandleDataPacket(packet);
                    break;
            }
        }

        private void HandleControlPacket(Packet packet)
        {
            base.HandlePacket(packet);

            while (true)
            {
                UInt16 len;
                PacketStream.State prevState = stream.CurrentState;

                try
                {
                    stream.Seek(2, SeekOrigin.Current);

                    len = stream.PeekU16();
                }
                catch (EndOfStreamException)
                {
                    if (stream.HasNextDirection())
                    {
                        stream.NextDirection();
                        prevState = stream.CurrentState;
                        continue;
                    }
                    else
                    {
                        return;
                    }
                }
                finally
                {
                    stream.CurrentState = prevState;
                }

                if (stream.Length - stream.Position >= len + 4)
                {
                    try
                    {
                        ParseControlCommand();
                    }
                    catch (StreamSessionParseError e)
                    {
                        logger.AddMessage(String.Format("Failed to parse RRAC command: {0}", e.Message));
                    }

                    stream.CurrentState = prevState;

                    stream.Seek(4 + len, SeekOrigin.Current);
                }
                else
                {
                    return;
                }
            }
        }

        private void ParseControlCommand()
        {
            List<PacketSlice> slices = new List<PacketSlice>(2);

            UInt16 cmdTypeRaw, cmdLen;
            RRACCommand cmdType;
            string cmdTypeStr;
            List<PacketSlice> cmdTypeSlices = new List<PacketSlice>(1);
            List<PacketSlice> cmdLenSlices = new List<PacketSlice>(1);

            UInt32 u32;
            byte[] bytes;

            try
            {
                // Command type
                cmdTypeRaw = stream.ReadU16(cmdTypeSlices);

                // Command length
                cmdLen = stream.ReadU16(cmdLenSlices);

                ParseCommandType(cmdTypeRaw, out cmdType, out cmdTypeStr);

                TransactionNode cmd = new TransactionNode(cmdTypeStr);

                cmd.AddField("CommandType", cmdTypeStr,
                    "Command type.", cmdTypeSlices);
                cmd.AddField("CommandLength", Convert.ToString(cmdLen),
                    "Number of bytes following this field.", cmdLenSlices);

                if (cmdType != RRACCommand.Response)
                {
                    cmd.Description = cmd.Name;
                }

                bool handled = true;

                switch (cmdType)
                {
                    case RRACCommand.GetMetaData:
                        u32 = stream.ReadU32(slices);
                        cmd.AddField("GetMask", Util.FormatFlags(u32),
                            "A bitmask specifying what metadata to get.", slices);

                        pendingReplies[cmdType] = new GetMetaDataCmd(u32);
                        break;
                    case RRACCommand.SetMetaData:
                        u32 = stream.ReadU32(slices);
                        cmd.AddField("PayloadSize", Convert.ToString(u32),
                            "The number of bytes of payload following this field.", slices);

                        u32 = stream.ReadU32(slices);
                        cmd.AddField("MagicValue", Util.FormatFlags(u32),
                            "Magic value that must be 0xF0000001.", slices);

                        u32 = stream.ReadU32(slices);
                        cmd.AddField("SetOid", FormatSetType(u32),
                            "The identifier of the object to set.", slices);

                        pendingReplies[cmdType] = new SetMetaDataCmd(u32);

                        switch (u32)
                        {
                            case SetMetaDataCmd.TYPE_BORING_SSPIDS:
                                bytes = stream.ReadBytes(16, slices);
                                cmd.AddField("FourUnknownDWORDs", Util.FormatByteArray(bytes),
                                    "Four unknown DWORDs.", slices);

                                UInt32 numObjects = stream.ReadU32(slices);
                                cmd.AddField("NumBoringObjects", Convert.ToString(numObjects),
                                    "Number of boring objects following this field.", slices);

                                TransactionNode boringObjects = new TransactionNode("BoringObjects");
                                cmd.AddChild(boringObjects);

                                for (int i = 0; i < numObjects; i++)
                                {
                                    u32 = stream.ReadU32(slices);
                                    boringObjects.AddField(Convert.ToString(i), Util.FormatFlags(u32),
                                        "Object type ID.", slices);
                                }
                                break;
                            default:
                                handled = false;
                                cmdLen -= 4 * 3;
                                break;
                        }
                        break;
                    case RRACCommand.Notify:
                        UInt32 notifyType = stream.ReadU32(slices);
                        cmd.AddField("NotifyType", FormatNotifyType(notifyType),
                            "Type of notification.", slices);

                        switch (notifyType)
                        {
                            case NotifyCmd.TYPE_PARTNERSHIPS:
                                u32 = stream.ReadU32(slices);
                                cmd.AddField("Unknown1", Util.FormatFlags(u32),
                                    "Unknown field 1.", slices);

                                u32 = stream.ReadU32(slices);
                                cmd.AddField("Unknown2", Util.FormatFlags(u32),
                                    "Unknown field 2.", slices);

                                u32 = stream.ReadU32(slices);
                                cmd.AddField("Size", Convert.ToString(u32),
                                    "Size of the remaining fields.", slices);

                                u32 = stream.ReadU32(slices);
                                cmd.AddField("PCur", Convert.ToString(u32),
                                    "Current partner index.", slices);

                                u32 = stream.ReadU32(slices);
                                cmd.AddField("P1", Util.FormatFlags(u32),
                                    "Partner 1 ID.", slices);

                                u32 = stream.ReadU32(slices);
                                cmd.AddField("P2", Util.FormatFlags(u32),
                                    "Partner 2 ID.", slices);
                                break;
                            default:
                                handled = false;
                                cmdLen -= 4;
                                break;
                        }
                        break;
                    case RRACCommand.Response:
                        ParseCommandReply(cmd, cmdLen);
                        break;
                    default:
                        handled = false;
                        break;
                }

                if (!handled)
                {
                    if (cmdLen > 0)
                    {
                        bytes = stream.ReadBytes((int)cmdLen, slices);
                        cmd.AddField("UnknownData", Util.FormatByteArray(bytes), "Unknown data.", slices);
                    }
                }

                AddTransactionNode(cmd);
            }
            catch (EndOfStreamException e)
            {
                throw new StreamSessionParseError(e.Message);
            }
        }

        private void HandleDataPacket(Packet packet)
        {
            base.HandlePacket(packet);
            
            logger.AddMessage(String.Format("Ignore packet {0} because of unhandled RRAC subprotocol (data)",
                packet.Index));
        }

        protected string FormatSetType(UInt32 type)
        {
            switch (type)
            {
                case SetMetaDataCmd.TYPE_BORING_SSPIDS:
                    return "BORING_OBJECTS";
                default:
                    return String.Format("UNKNOWN_{0:x2}", type);
            }
        }

        protected string FormatNotifyType(UInt32 type)
        {
            switch (type)
            {
                case NotifyCmd.TYPE_PARTNERSHIPS:
                    return "PARTNERSHIPS";
                default:
                    return String.Format("UNKNOWN_{0:x2}", type);
            }
        }

        protected void ParseCommandReply(TransactionNode cmd, int cmdLen)
        {
            UInt32 u32;
            byte[] bytes;
            string str;
            List<PacketSlice> slices = new List<PacketSlice>(2);

            UInt32 replyCmdTypeRaw;
            RRACCommand replyCmdType;
            string replyCmdTypeStr;

            replyCmdTypeRaw = stream.ReadU32(slices);

            ParseCommandType((UInt16)replyCmdTypeRaw, out replyCmdType, out replyCmdTypeStr);

            cmd.AddField("ReplyToCommand", replyCmdTypeStr,
                "Command which this is a reply to.", slices);

            int replyLen = cmdLen - 4;

            if (replyCmdType != RRACCommand.UNKNOWN)
            {
                if (!pendingReplies.ContainsKey(replyCmdType))
                    throw new StreamSessionParseError(String.Format(
                        "Got reply to a command we don't know about: {0} ({1})",
                        replyCmdType, replyCmdTypeRaw));
            }

            bool handled = true;

            cmd.Description = String.Format("{0} to {1}", cmd.Name, replyCmdTypeStr);

            u32 = stream.ReadU32(slices);
            cmd.AddField("Result", Util.FormatValue(u32),
                "Result of the request.", slices);
            
            u32 = stream.ReadU32(slices);
            cmd.AddField("ResponseDataSize", Util.FormatValue(u32),
                "Number of bytes of response data following the next field.", slices);

            u32 = stream.ReadU32(slices);
            cmd.AddField("Unknown1", Util.FormatValue(u32),
                "Unknown field.", slices);

            replyLen -= 3 * 4;

            if (replyCmdType == RRACCommand.GetMetaData)
            {
                GetMetaDataCmd listCmd =
                    pendingReplies[replyCmdType] as GetMetaDataCmd;

                switch (listCmd.Flags)
                {
                    case 0x7c1:
                        u32 = stream.ReadU32(slices);
                        cmd.AddField("MagicValue", Util.FormatFlags(u32),
                            "Magic value that must be 0xF0000001.", slices);

                        u32 = stream.ReadU32(slices);
                        cmd.AddField("Success", Util.FormatBool(u32),
                            "A value that must be TRUE, probably signifying whether the command succeeded.", slices);

                        u32 = stream.ReadU32(slices);
                        cmd.AddField("HasBody", Util.FormatBool(u32),
                            "A BOOL indicating if the response contains a body or is empty.", slices);

                        u32 = stream.ReadU32(slices);
                        cmd.AddField("ResponseTo", Util.FormatValue(u32),
                            "Two to the power of the position of the bit to which this is a response. So for instance " +
                            "if this is the response to bit 4, this field contains the value 16.", slices);

                        UInt32 typeCount = stream.ReadU32(slices);
                        cmd.AddField("TypeCount", Convert.ToString(typeCount),
                            "Number of types following.", slices);

                        TransactionNode objTypes = new TransactionNode("ObjectTypes");
                        cmd.AddChild(objTypes);

                        for (int i = 0; i < typeCount; i++)
                        {
                            TransactionNode node = new TransactionNode(
                                Convert.ToString(i));

                            u32 = stream.ReadU32(slices);
                            node.AddField("Flags", Util.FormatValue(u32),
                                "Unknown.", slices);

                            str = stream.ReadCString(200, slices);
                            node.AddField("Name1", str, "Name of the object type.", slices);

                            str = stream.ReadCString(80, slices);
                            node.AddField("Name2", str, "Name/description of the object type.", slices);

                            str = stream.ReadCString(80, slices);
                            node.AddField("Name3", str, "Another name/description of the object type (usually blank).", slices);

                            u32 = stream.ReadU32(slices);
                            node.AddField("SSPId", Util.FormatFlags(u32),
                                "Identifier of the object type.", slices);

                            u32 = stream.ReadU32(slices);
                            node.AddField("Count", Util.FormatValue(u32),
                                "Number of items of this object type on the device.", slices);

                            u32 = stream.ReadU32(slices);
                            node.AddField("TotalSize", Util.FormatValue(u32),
                                "Total size of the items of this object type on the device.", slices);

                            List<PacketSlice> allSlices = new List<PacketSlice>(2);

                            UInt32 timeLow = stream.ReadU32(slices);
                            allSlices.AddRange(slices);

                            UInt32 timeHigh = stream.ReadU32(slices);
                            allSlices.AddRange(slices);

                            UInt64 time = (UInt64)timeLow & ((UInt64)timeHigh << 32);

                            node.AddField("FileTime", String.Format("0x{0:x16}", time),
                                "Time of last modification of any of the items of this object type on the device (?).", allSlices);

                            objTypes.AddChild(node);
                        }

                        break;
                    default:
                        handled = false;
                        break;
                }
            }
            else
            {
                handled = false;
            }

            if (!handled)
            {
                if (replyLen > 0)
                {
                    bytes = stream.ReadBytes(replyLen, slices);
                    cmd.AddField("UnknownReplyData", Util.FormatByteArray(bytes),
                        "Unknown reply data.", slices);
                }
            }

            pendingReplies.Remove(replyCmdType);
        }

        private void ParseCommandType(UInt16 cmdTypeRaw,
                                      out RRACCommand cmdType,
                                      out string cmdTypeStr)
        {
            if (knownCommands.ContainsKey(cmdTypeRaw))
            {
                cmdType = knownCommands[cmdTypeRaw];
                cmdTypeStr = String.Format("RRAC {0}",
                    cmdType.ToString(), cmdTypeRaw);
            }
            else
            {
                cmdType = RRACCommand.UNKNOWN;
                cmdTypeStr = String.Format("RRAC UNKNOWN{0}", cmdTypeRaw);
            }
        }
    }

    public class GetMetaDataCmd : TransactionNode
    {
        protected UInt32 flags;
        public UInt32 Flags
        {
            get { return flags; }
        }

        public GetMetaDataCmd(UInt32 flags)
            : base("LIST")
        {
            this.flags = flags;
        }
    }

    public class SetMetaDataCmd : TransactionNode
    {
        protected UInt32 type;
        public UInt32 Type
        {
            get { return type; }
        }

        public const int TYPE_BORING_SSPIDS = 3;

        public SetMetaDataCmd(UInt32 type)
            : base("SET")
        {
            this.type = type;
        }
    }

    public class NotifyCmd : TransactionNode
    {
        public const int TYPE_PARTNERSHIPS = 0x02000000;

        public NotifyCmd()
            : base("NOTIFY")
        {
        }
    }
}

#endif