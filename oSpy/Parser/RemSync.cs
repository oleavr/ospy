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
    public class RemSyncTransactionFactory : StreamTransactionFactory
    {
        public const int REMSYNC_PORT = 999;

        public RemSyncTransactionFactory(DebugLogger logger)
            : base(new int[] { REMSYNC_PORT }, null, logger)
        {
        }

        protected override StreamSession CreateSession(Packet firstPacket)
        {
            return new RemSyncSession(logger);
        }
    }

    public class RemSyncSession : StreamSession
    {
        private enum RemSyncCommand
        {
            SetStatus,
            Unknown_04,
            Unknown_0a,
            StartOfSync,
            EndOfSync,
            SetProgressRange,
            SetProgressValue,
            UNKNOWN
        };

        private static Dictionary<UInt16, RemSyncCommand> knownCommands;

        static RemSyncSession()
        {
            knownCommands = new Dictionary<ushort, RemSyncCommand>();

            knownCommands[0x01] = RemSyncCommand.SetStatus;
            knownCommands[0x04] = RemSyncCommand.Unknown_04;
            knownCommands[0x0a] = RemSyncCommand.Unknown_0a;
            knownCommands[0x5a] = RemSyncCommand.StartOfSync;
            knownCommands[0x5b] = RemSyncCommand.EndOfSync;
            knownCommands[0x5c] = RemSyncCommand.SetProgressRange;
            knownCommands[0x5d] = RemSyncCommand.SetProgressValue;
        }

        public RemSyncSession(DebugLogger logger)
            : base(logger)
        {
        }

        public override void HandlePacket(Packet packet)
        {
            base.HandlePacket(packet);

            while (true)
            {
                PacketStream.State prevState = stream.CurrentState;

                try
                {
                    List<PacketSlice> slices = new List<PacketSlice>(2);

                    TransactionNode node;

                    if (stream.Direction == PacketDirection.PACKET_DIRECTION_OUTGOING)
                    {
                        RemSyncCommand cmdType;
                        string cmdTypeStr;

                        List<PacketSlice> argSlices = new List<PacketSlice>(2);
                        UInt16 arg = stream.ReadU16(argSlices);

                        UInt16 cmdTypeRaw = stream.ReadU16(slices);

                        ParseCommandType(cmdTypeRaw, out cmdType, out cmdTypeStr);

                        node = new TransactionNode(cmdTypeStr);

                        switch (cmdType)
                        {
                            case RemSyncCommand.SetStatus:
                                node.AddField("MessageSize", Convert.ToString(arg),
                                    "Size of zero-terminated unicode string following the next field.",
                                    argSlices);
                                break;
                            case RemSyncCommand.SetProgressValue:
                                node.AddField("NewValue", Convert.ToString(arg),
                                    "The new progress value, within ProgressMin and ProgressMax as specified with SetProgressRange.",
                                    argSlices);
                                break;
                            default:
                                node.AddField("Argument", Convert.ToString(arg),
                                    "Argument, meaning depending on the NotificationType field.",
                                    argSlices);
                                break;
                        }

                        node.AddField("NotificationType", cmdTypeStr, "Type of notification.", slices);

                        UInt16 u16;

                        switch (cmdType)
                        {
                            case RemSyncCommand.SetStatus:
                                string msg = stream.ReadCString(arg, slices);
                                node.AddField("MessageBytes", msg,
                                    "A zero-terminated unicode LE16 string with the size as specified by MessageSize (which includes the terminating NUL byte).",
                                    slices);
                                break;
                            case RemSyncCommand.SetProgressRange:
                                u16 = stream.ReadU16(slices);
                                node.AddField("ProgressMin", Convert.ToString(u16),
                                    "Progressbar min value.", slices);

                                u16 = stream.ReadU16(slices);
                                node.AddField("ProgressMax", Convert.ToString(u16),
                                    "Progressbar max value.", slices);
                                break;
                        }
                    }
                    else
                    {
                        node = new TransactionNode("RemSync::UNKNOWN_REPLY");

                        byte[] bytes = stream.ReadBytes((int)stream.Length, slices);
                        node.AddField("UnknownBytes", Util.FormatByteArray(bytes), "Unknown reply data.", slices);
                    }

                    node.Description = node.Name;

                    AddTransactionNode(node);
                }
                catch (EndOfStreamException)
                {
                    if (stream.HasNextDirection())
                    {
                        stream.NextDirection();
                        continue;
                    }
                    else
                    {
                        stream.CurrentState = prevState;
                        return;
                    }
                }
            }
        }

        private void ParseCommandType(UInt16 cmdTypeRaw,
                                      out RemSyncCommand cmdType,
                                      out string cmdTypeStr)
        {
            if (knownCommands.ContainsKey(cmdTypeRaw))
            {
                cmdType = knownCommands[cmdTypeRaw];
                cmdTypeStr = String.Format("RemSync {0}", cmdType.ToString());
            }
            else
            {
                cmdType = RemSyncCommand.UNKNOWN;
                cmdTypeStr = String.Format("RemSync UNKNOWN{0}", cmdTypeRaw);
            }
        }
    }
}

#endif