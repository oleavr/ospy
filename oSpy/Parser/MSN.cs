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
using System.IO;
using Flobbster.Windows.Forms;
using System.Runtime.Serialization;
using System.Drawing;
using oSpy.Util;
using oSpy.Net;
namespace oSpy.Parser
{
    [Serializable()]
    public class MSNP2PMessage : VisualTransaction
    {
        private UInt32 messageID;
        public UInt32 MessageID
        {
            get { return messageID; }
        }

        private UInt32 sessionID;
        public UInt32 SessionID
        {
            get { return sessionID; }
        }

        private UInt32 flags;
        public UInt32 Flags
        {
            get { return flags; }
        }

        private UInt64 initialOffset;
        public UInt64 InitialOffset
        {
            get { return initialOffset; }
        }

        private UInt64 transferred;
        public UInt64 Transferred
        {
            get { return transferred; }
            set { transferred = value; }
        }

        private UInt64 dataSize;
        public UInt64 DataSize
        {
            get { return dataSize; }
        }

        private UInt32 ackedMsgID;
        public UInt32 AckedMsgID
        {
            get { return ackedMsgID; }
        }

        private UInt32 prevAckedMsgID;
        public UInt32 PrevAckedMsgID
        {
            get { return prevAckedMsgID; }
        }

        private UInt64 ackedDataSize;
        public UInt64 AckedDataSize
        {
            get { return ackedDataSize; }
        }

        public MemoryStream PreviewData
        {
            get
            {
                if (!previewData.ContainsKey(messageID))
                    previewData[messageID] = new MemoryStream();

                return previewData[messageID];
            }
        }

        protected static Dictionary<string, MSNSLPCall> cidToCall = new Dictionary<string, MSNSLPCall>();
        protected static Dictionary<UInt32, MSNSLPCall> sidToCall = new Dictionary<UInt32, MSNSLPCall>();
        protected static Dictionary<UInt32, MemoryStream> previewData = new Dictionary<UInt32, MemoryStream>();

        public MSNP2PMessage(int index, PacketDirection direction, DateTime startTime,
                             UInt32 messageID, UInt32 sessionID, UInt32 flags,
                             UInt64 initialOffset, UInt64 dataSize,
                             UInt32 ackedMsgID, UInt32 prevAckedMsgID,
                             UInt64 ackedDataSize)
            : base(index, direction, startTime)
        {
            this.messageID = messageID;
            this.sessionID = sessionID;
            this.flags = flags;
            this.initialOffset = initialOffset;
            this.transferred = 0;
            this.dataSize = dataSize;
            this.ackedMsgID = ackedMsgID;
            this.prevAckedMsgID = prevAckedMsgID;
            this.ackedDataSize = ackedDataSize;
        }

        public MSNP2PMessage(SerializationInfo info, StreamingContext ctx)
            : base(info, ctx)
        {
            messageID = (UInt32)info.GetValue("messageID", typeof(UInt32));
            sessionID = (UInt32)info.GetValue("sessionID", typeof(UInt32));
            flags = (UInt32)info.GetValue("flags", typeof(UInt32));
            initialOffset = (UInt64)info.GetValue("initialOffset", typeof(UInt64));
            transferred = (UInt64)info.GetValue("transferred", typeof(UInt64));
            dataSize = (UInt64)info.GetValue("dataSize", typeof(UInt64));
            ackedMsgID = (UInt32)info.GetValue("ackedMsgID", typeof(UInt32));
            prevAckedMsgID = (UInt32)info.GetValue("prevAckedMsgID", typeof(UInt32));
            ackedDataSize = (UInt64)info.GetValue("ackedDataSize", typeof(UInt64));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            base.GetObjectData(info, ctx);

            info.AddValue("messageID", messageID);
            info.AddValue("sessionID", sessionID);
            info.AddValue("flags", flags);
            info.AddValue("initialOffset", initialOffset);
            info.AddValue("transferred", transferred);
            info.AddValue("dataSize", dataSize);
            info.AddValue("ackedMsgID", ackedMsgID);
            info.AddValue("prevAckedMsgID", prevAckedMsgID);
            info.AddValue("ackedDataSize", ackedDataSize);
        }

        public override void TransactionsCreated()
        {
            AddHeaderField("Direction", Direction);

            AddHeaderField("MessageID", messageID);
            AddHeaderField("SessionID", sessionID);
            AddHeaderField("Flags", String.Format("0x{0:x8}", flags));
            AddHeaderField("DataOffset", initialOffset);
            string str = Convert.ToString(dataSize);
            if (initialOffset > 0 || transferred < dataSize)
                str += String.Format(" ({0} ITS)", transferred);
            AddHeaderField("DataSize", str);
            AddHeaderField("AckedMsgID", ackedMsgID);
            AddHeaderField("PrevAckedMsgID", prevAckedMsgID);
            AddHeaderField("AckedDataSize", ackedDataSize);
        }

        public override void SessionsCreated()
        {
            string str;
            int n;

            HeaderRowsPerCol = 5;

            MSNSLPCall call = null;

            if (sessionID == 0)
            {
                byte[] bytes = PreviewData.ToArray();
                if (bytes.Length > 0)
                {
                    str = StaticUtils.DecodeUTF8(bytes);
                    if (str[str.Length - 1] == '\0')
                    {
                        str = str.Substring(0, str.Length - 1);
                        str += "\\0";
                    }

                    BodyText = str;

                    string[] firstLineAndRest = str.Split(lineDelimiters, 2, StringSplitOptions.None);

                    Dictionary<string, string> slpHeaderFields, sessionHeaderFields;
                    string slpBody, sessionBody;

                    ParseHTTPStyle(firstLineAndRest[1], out slpHeaderFields, out slpBody);
                    ParseHTTPStyle(slpBody, out sessionHeaderFields, out sessionBody);

                    if (slpHeaderFields.ContainsKey("CALL-ID") &&
                        sessionHeaderFields.ContainsKey("SESSIONID"))
                    {
                        string cid = slpHeaderFields["CALL-ID"];
                        UInt32 sid = Convert.ToUInt32(sessionHeaderFields["SESSIONID"]);

                        if (cidToCall.ContainsKey(cid))
                        {
                            call = cidToCall[cid];
                        }
                        else
                        {
                            call = new MSNSLPCall(cid);
                            cidToCall[cid] = call;
                            sidToCall[sid] = call;
                        }
                    }
                }
            }
            else
            {
                if (sidToCall.ContainsKey(sessionID))
                {
                    call = sidToCall[sessionID];
                }

                if (transferred > 0)
                {
                    n = (int)transferred;
                    if (n > 128)
                        n = 128;

                    byte[] data = new byte[n];
                    PreviewData.Position = (long) initialOffset;
                    PreviewData.Read(data, 0, data.Length);

                    int remaining = 0;
                    if ((int)transferred > data.Length)
                        remaining = (int)transferred - data.Length;

                    SetBodyFromTruncatedPreviewData(data, remaining);

                    if (flags == 0x20 && initialOffset + transferred >= dataSize)
                    {
                        PreviewData.Position = 0;

                        try
                        {
                            PreviewImage = new Bitmap(PreviewData);
                        }
                        catch (ArgumentException) {}
                    }
                }
            }

            if (call != null)
            {
                ContextID = call.CallID;
            }
        }

        protected static int unknownImgCount = 0;

        protected static string[] lineDelimiters = new string[] { "\r\n", "\n" };
        protected static string[] dblLineDelimiters = new string[] { "\r\n\r\n", "\n\n" };

        private void ParseHTTPStyle(string str,
                                    out Dictionary<string, string> headerFields,
                                    out string body)
        {

            string[] headersAndBody = str.Split(dblLineDelimiters, 2, StringSplitOptions.None);

            string[] headerLines = headersAndBody[0].Split(lineDelimiters, StringSplitOptions.RemoveEmptyEntries);

            headerFields = new Dictionary<string, string>(headerLines.Length);
            foreach (string line in headerLines)
            {
                string[] keyValue = line.Split(new char[] { ':' }, 2);
                if (keyValue.Length > 1)
                    headerFields[keyValue[0].ToUpper()] = keyValue[1].TrimStart();
            }

            if (headersAndBody.Length > 1)
                body = headersAndBody[1];
            else
                body = "";
        }
    }

    public class MSNSBVisualizer : SessionVisualizer
    {
        public override string Name
        {
            get { return "MSNSwitchboard"; }
        }

        public override VisualTransaction[] GetTransactions(IPSession session)
        {
            List<VisualTransaction> messages = new List<VisualTransaction>();

            foreach (TransactionNode node in session.Nodes)
            {
                if (node.Name == "MSNSBCommand")
                {
                    IPPacket pkt = node.Slices[0].Packet;

                    VisualTransaction vt = new VisualTransaction(node.Index, pkt.Direction, pkt.Timestamp);

                    string headline = (string) node["Command"];

                    if (node.Fields.ContainsKey("Arguments"))
                        headline += " " + (string) node["Arguments"];

                    vt.HeadlineText = headline;

                    TransactionNode payloadNode = node.FindChild("Payload", false);
                    if (payloadNode != null)
                    {
                        string body = "";

                        if (payloadNode.Fields.ContainsKey("XML"))
                        {
                            XMLHighlighter highlighter;

                            XML.PrettyPrint((string)payloadNode["XML"], out body, out highlighter);
                        }
                        else if (payloadNode.Fields.ContainsKey("Text"))
                        {
                            body = (string)payloadNode["Text"];
                        }
                        else if (payloadNode.Fields.ContainsKey("MSNSLP"))
                        {
                            body = (string)payloadNode["MSNSLP"];
                        }
                        else
                        {
                            body = String.Format("Unhandled payload format: {0}",
                                (payloadNode.FieldNames.Count > 0) ? payloadNode.FieldNames[0] : payloadNode.Children[0].Name);
                        }

                        vt.BodyText = body;
                    }

                    messages.Add(vt);
                }
            }

            return messages.ToArray();
        }
    }

    [Serializable()]
    public class MSNSLPCall
    {
        protected string callID;
        public string CallID
        {
            get { return callID; }
        }

        protected Color color;
        public Color Color
        {
            get { return color; }
        }

        protected static ColorPool colorPool = new ColorPool(true);

        public MSNSLPCall(string callID)
        {
            this.callID = callID;

            color = colorPool.GetColorForId(callID);
        }
    }

    public class MSNP2PVisualizer : SessionVisualizer
    {
        public override string Name
        {
            get { return "MSNP2P"; }
        }

        public override VisualTransaction[] GetTransactions(IPSession session)
        {
            List<MSNP2PMessage> messages = new List<MSNP2PMessage>();
            Dictionary<UInt32, MSNP2PMessage> messageFromId = new Dictionary<UInt32, MSNP2PMessage>();

            foreach (TransactionNode node in session.Nodes)
            {
                if (node.Name.StartsWith("MSN"))
                {
                    TransactionNode chunk = node.FindChild("MSNP2PMessageChunk");
                    if (chunk != null)
                    {
                        MSNP2PMessage msg;

                        TransactionNode headers = chunk.FindChild("Headers");

                        UInt32 msgID = (UInt32)headers.Fields["MessageID"];
                        UInt32 chunkSize = (UInt32)headers.Fields["ChunkSize"];

                        if (messageFromId.ContainsKey(msgID))
                        {
                            msg = messageFromId[msgID];
                        }
                        else
                        {
                            PacketDirection direction =
                                headers.GetSlicesForFieldPath("SessionID")[0].Packet.Direction;

                            UInt32 sessionID = (UInt32)headers.Fields["SessionID"];
                            UInt32 flags = (UInt32)headers.Fields["Flags"];
                            UInt64 dataOffset = (UInt64)headers.Fields["DataOffset"];
                            UInt64 dataSize = (UInt64)headers.Fields["DataSize"];
                            UInt32 ackedMsgID = (UInt32)headers.Fields["AckedMsgID"];
                            UInt32 prevAckedMsgID = (UInt32)headers.Fields["PrevAckedMsgID"];
                            UInt64 ackedDataSize = (UInt64)headers.Fields["AckedDataSize"];

                            msg = new MSNP2PMessage(chunk.Index, direction, chunk.StartTime,
                                msgID, sessionID, flags, dataOffset, dataSize, ackedMsgID,
                                prevAckedMsgID, ackedDataSize);
                            messages.Add(msg);
                            messageFromId[msgID] = msg;
                        }

                        int maxPreview = 4096;

                        if (msg.Flags == 0x20)
                        {
                            maxPreview = 131072;
                        }

                        if (chunkSize > 0 && msg.Transferred < (ulong) maxPreview)
                        {
                            TransactionNode content = chunk.FindChild("Content");
                            string fieldName = (msg.SessionID != 0) ? "Raw" : "MSNSLP";
                            byte[] bytes = (byte[])content.Fields[fieldName];

                            int n = bytes.Length;
                            int max = maxPreview - (int) msg.Transferred;
                            if (n > max)
                                n = max;

                            msg.PreviewData.Write(bytes, 0, bytes.Length);
                        }

                        msg.EndTime = chunk.EndTime;

                        msg.Transferred += chunkSize;
                    }
                }
            }

            return messages.ToArray();
        }
    }

    public class MSNTransactionFactory : TransactionFactory
    {
        public const int MSN_SB_PORT = 1863;

        protected enum PayloadFormat {
            TEXT,
            XML,
            MESSAGE,
            SLP,
        };

        protected static List<string> payloadCommandsFromClient;
        protected static List<string> payloadCommandsFromServer;
        protected static Dictionary<string, PayloadFormat> payloadCommandFormats;

        static MSNTransactionFactory()
        {
            // FIXME: add all of them here
            payloadCommandsFromClient = new List<string>(new string[] {
                "MSG", "UBX", "UUX", "ADL", "RML", "FQY", "QRY", "GCF", "FQY", "NOT",
                "UUN", "UBN",
            });

            payloadCommandsFromServer = new List<string>(new string[] {
                "MSG", "UBX", "UUX", "FQY", "GCF", "FQY", "NOT",
                "UUN", "UBN",
            });

            payloadCommandFormats = new Dictionary<string, PayloadFormat>();
            payloadCommandFormats["MSG"] = PayloadFormat.MESSAGE;
            payloadCommandFormats["NOT"] = PayloadFormat.MESSAGE;
            payloadCommandFormats["UBX"] = PayloadFormat.XML;
            payloadCommandFormats["UUX"] = PayloadFormat.XML;
            payloadCommandFormats["ADL"] = PayloadFormat.XML;
            payloadCommandFormats["RML"] = PayloadFormat.XML;
            payloadCommandFormats["GCF"] = PayloadFormat.XML;
            payloadCommandFormats["UUN"] = PayloadFormat.SLP;
            payloadCommandFormats["UBN"] = PayloadFormat.SLP;
        }

        public MSNTransactionFactory(DebugLogger logger)
            : base(logger)
        {
        }
        public override string Name() {
            return "MSN Transaction Factory";
        }
        public override bool HandleSession(IPSession session)
        {
            logger.AddMessage(String.Format("session.LocalEndpoint.Port={0}, session.RemoteEndpoint.Port={1}",
                session.LocalEndpoint.Port, session.RemoteEndpoint.Port));
            if (session.RemoteEndpoint.Port == MSN_SB_PORT)
                return HandleSwitchboardSession(session);

            PacketStream stream = session.GetNextStreamDirection();

            if (stream.GetBytesAvailable() < 8)
                return false;

            List<PacketSlice> lenSlices = new List<PacketSlice>(1);
            UInt32 len = stream.ReadU32LE(lenSlices);
            if (len != 4)
                return false;

            List<PacketSlice> contentSlices = new List<PacketSlice>(1);
            string str = stream.ReadCStringASCII((int) len, contentSlices);
            if (str != "foo")
                return false;

            TransactionNode magicNode = new TransactionNode("MSNP2PDirectMagic");
            magicNode.Description = magicNode.Name;

            magicNode.AddField("Length", len, "Magic length.", lenSlices);
            magicNode.AddField("Magic", str, "Magic string.", contentSlices);

            TransactionNode requestNode = ReadNextP2PDirectMessage(stream, "Request");

            stream = session.GetNextStreamDirection();
            TransactionNode responseNode = ReadNextP2PDirectMessage(stream, "Response");

            TransactionNode handshakeNode = new TransactionNode("MSNP2PDirectHandshake");
            handshakeNode.Description = handshakeNode.Name;
            handshakeNode.AddChild(requestNode);
            handshakeNode.AddChild(responseNode);

            session.AddNode(magicNode);
            session.AddNode(handshakeNode);

            ReadAllP2PDirectMessages(session, session.GetNextStreamDirection());
            ReadAllP2PDirectMessages(session, session.GetNextStreamDirection());

            return true;
        }

        private void ReadAllP2PDirectMessages(IPSession session, PacketStream stream)
        {
            while (stream.GetBytesAvailable() > 0)
            {
                try
                {
                    TransactionNode node = ReadNextP2PDirectMessage(stream, "MSNP2PDirectMessage");
                    session.AddNode(node);
                }
                catch (EndOfStreamException e)
                {
                    logger.AddMessage(String.Format("MSNP2PDirect: EOS at {0} ({1})", stream.Position, e));
                    break;
                }
            }
        }

        private TransactionNode ReadNextP2PDirectMessage(PacketStream stream, string name)
        {
            TransactionNode msgNode = new TransactionNode(name);
            msgNode.Description = msgNode.Name;

            List<PacketSlice> slices = new List<PacketSlice>(1);
            uint len = stream.ReadU32LE(slices);
            msgNode.AddField("MSNP2PLength", len, "P2P message chunk length.", slices);

            ReadNextP2PMessageChunk(stream, msgNode);

            return msgNode;
        }

        private bool HandleSwitchboardSession(IPSession session)
        {
            List<PacketSlice> slices = new List<PacketSlice>(1);

            logger.AddMessage(String.Format("\r\n\r\nparsing session with remote endpoint: {0}\r\n", session.RemoteEndpoint));

            while (true)
            {
                PacketStream stream = session.GetNextStreamDirection();

                if (stream.GetBytesAvailable() == 0)
                {
                    stream = session.GetNextStreamDirection();
                    if (stream.GetBytesAvailable() == 0)
                    {
                        break;
                    }
                }

                IPPacket pkt = stream.CurPacket;
                PacketDirection direction = pkt.Direction;
                
                try
                {
                    string line = stream.PeekLineUTF8();

                    // Split the line up into CMD and the rest (being arguments, if any)
                    string[] tokens = line.Split(new char[] { ' ' }, 2);

                    logger.AddMessage(String.Format("{0} parsing command '{1}' (line: {2})",
                        (direction == PacketDirection.PACKET_DIRECTION_INCOMING) ? "<<" : ">>",
                        tokens[0], line));

                    // Set cmd and create an array of arguments if present
                    string cmd = tokens[0];
                    string[] arguments = new string[0];
                    if (tokens.Length > 1)
                    {
                        arguments = tokens[1].Split(new char[] { ' ' });
                    }

                    // Create command node
                    TransactionNode node = new TransactionNode("MSNSBCommand");
                    node.Description = cmd;

                    // Command field
                    stream.ReadBytes(StaticUtils.GetUTF8ByteCount(tokens[0]), slices);
                    node.AddField("Command", tokens[0], "Switchboard command.", slices);

                    if (arguments.Length > 0)
                    {
                        // Skip space between command and arguments
                        stream.ReadByte();

                        stream.ReadBytes(StaticUtils.GetUTF8ByteCount(tokens[1]), slices);

                        // Arguments fields
                        node.AddField("Arguments", tokens[1], "Arguments to command.", slices);
                    }

                    // Skip CRLF
                    stream.ReadBytes(2);

                    // Is there a payload?
                    bool hasPayload = false;
                    if (arguments.Length > 0)
                    {
                        List<string> payloadCommands =
                            (direction == PacketDirection.PACKET_DIRECTION_OUTGOING) ? payloadCommandsFromClient : payloadCommandsFromServer;

                        hasPayload = payloadCommands.Contains(cmd);
                    }

                    if (hasPayload)
                    {
                        int payloadLength = -1;

                        try
                        {
                            payloadLength = (int)Convert.ToUInt32(arguments[arguments.Length - 1]);
                        }
                        catch (FormatException)
                        {
                        }

                        if (payloadLength > 0)
                        {
                            TransactionNode payloadNode = new TransactionNode(node, "Payload");

                            logger.AddMessage(String.Format("Parsing {0} bytes of payload", payloadLength));

                            PayloadFormat format = PayloadFormat.TEXT;

                            string cmdUpper = cmd.ToUpper();
                            if (payloadCommandFormats.ContainsKey(cmdUpper))
                                format = payloadCommandFormats[cmdUpper];

                            if (format == PayloadFormat.MESSAGE)
                            {
                                SBParseMSG(stream, payloadNode, payloadLength);
                            }
                            else
                            {
                                string body = stream.ReadStringUTF8(payloadLength, slices);

                                switch (format)
                                {
                                    case PayloadFormat.SLP:
                                        payloadNode.AddTextField("MSNSLP", body, "MSNSLP data.", slices);
                                        break;
                                    case PayloadFormat.XML:
                                        payloadNode.AddXMLField("XML", body, "XML data.", slices);
                                        break;
                                    default:
                                        payloadNode.AddTextField("Text", body, "Text.", slices);
                                        break;
                                }
                            }
                        }
                    }

                    session.AddNode(node);
                }
                catch (EndOfStreamException e)
                {
                    logger.AddMessage(String.Format("MSNSwitchboard: EOS at {0} ({1})", stream.Position, e));
                    break;
                }
            }

            logger.AddMessage("done with session\r\n\r\n");

            return true;
        }

        protected void SBParseMSG(PacketStream stream, TransactionNode payloadNode, int payloadLength)
        {
            List<PacketSlice> slices = new List<PacketSlice>(2);

            string content = stream.PeekStringUTF8(payloadLength);

            TransactionNode headersNode = new TransactionNode(payloadNode, "Headers");

            int pos = content.IndexOf("\r\n\r\n");
            string headers = content.Substring(0, pos);
            string[] lines = headers.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                stream.ReadBytes(StaticUtils.GetUTF8ByteCount(line), slices);

                string[] tokens = line.Split(new char[] { ':' }, 2);
                tokens[1] = tokens[1].TrimStart(new char[] { ' ' });

                headersNode.AddField(tokens[0], tokens[1], "Message header field.", slices);

                // Skip CRLF
                stream.ReadBytes(2);
            }

            // Skip extra CRLF
            stream.ReadBytes(2);

            int bodyLength = payloadLength - StaticUtils.GetUTF8ByteCount(headers) - 4;

            if (bodyLength > 0)
            {
                TransactionNode bodyNode = new TransactionNode(payloadNode, "Body");

                string contentType = (string)headersNode.Fields["Content-Type"];
                contentType = contentType.Split(new char[] { ';' }, 2)[0];

                if (contentType == "application/x-msnmsgrp2p")
                {
                    ReadNextP2PMessageChunk(stream, bodyNode);

                    UInt32 appID = stream.ReadU32BE(slices);
                    bodyNode.AddField("AppID", appID, "Application ID.", slices);
                }
                else if (contentType == "text/x-msmsgsinvite")
                {
                    string bodyStr = stream.ReadStringUTF8(bodyLength, slices);

                    bodyNode.AddTextField("Body", bodyStr, "Invite body.", slices);
                }
                else
                {
                    string bodyStr = stream.ReadStringUTF8(bodyLength, slices);

                    bodyNode.AddField("Body", bodyStr, "Body.", slices);
                }
            }
        }

        protected void ReadNextP2PMessageChunk(PacketStream stream, TransactionNode parentNode)
        {
            TransactionNode p2pNode = new TransactionNode(parentNode, "MSNP2PMessageChunk");

            // Headers
            TransactionNode p2pHeaders = new TransactionNode(p2pNode, "Headers");

            List<PacketSlice> slices = new List<PacketSlice>();

            UInt32 sessionID = stream.ReadU32LE(slices);
            p2pHeaders.AddField("SessionID", sessionID, "Session ID.", slices);

            UInt32 messageID = stream.ReadU32LE(slices);
            p2pHeaders.AddField("MessageID", messageID, "Message ID.", slices);

            UInt64 dataOffset = stream.ReadU64LE(slices);
            p2pHeaders.AddField("DataOffset", dataOffset, "Data offset.", slices);

            UInt64 dataSize = stream.ReadU64LE(slices);
            p2pHeaders.AddField("DataSize", dataSize, "Data size.", slices);

            UInt32 chunkSize = stream.ReadU32LE(slices);
            p2pHeaders.AddField("ChunkSize", chunkSize, "Chunk size.", slices);

            UInt32 flags = stream.ReadU32LE(slices);
            p2pHeaders.AddField("Flags", flags, StaticUtils.FormatFlags(flags), "Flags.", slices);

            UInt32 ackedMsgID = stream.ReadU32LE(slices);
            p2pHeaders.AddField("AckedMsgID", ackedMsgID, "MessageID of the message to be acknowledged.", slices);

            UInt32 prevAckedMsgID = stream.ReadU32LE(slices);
            p2pHeaders.AddField("PrevAckedMsgID", prevAckedMsgID, "AckedMsgID of the last chunk to ack.", slices);

            UInt64 ackedDataSize = stream.ReadU64LE(slices);
            p2pHeaders.AddField("AckedDataSize", ackedDataSize, "Acknowledged data size.", slices);

            // Body
            TransactionNode p2pContent = null;
            if (chunkSize > 0)
            {
                p2pContent = new TransactionNode(p2pNode, "Content");

                byte[] bytes = stream.ReadBytes((int)chunkSize, slices);

                if (sessionID != 0)
                {
                    p2pContent.AddField("Raw", bytes, StaticUtils.FormatByteArray(bytes), "Raw content.", slices);
                }
                else
                {
                    p2pContent.AddTextField("MSNSLP", bytes, StaticUtils.DecodeUTF8(bytes), "MSNSLP data.", slices);
                }
            }
        }
    }
}
