/**
 * Copyright (C) 2006  Frode Hus <husfro@gmail.com>
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
using System.Diagnostics;
using System.Text.RegularExpressions;
using oSpy.Parser;
using oSpy.Util;
using oSpy.Net;
namespace oSpy {
    public class OracleTransaction : TransactionFactory {
        private int redirPort;
        
        public enum OracleTransactionType {
            CONNECT = 1,
            ACCEPT = 2,
            ACK = 3,
            REFUSE = 4,
            NULL = 7,
            ABORT = 9,
            DATA = 6,
            REDIRECT = 5,
            RESEND = 11,
            MARKER = 12,
            ATTENTION = 13,
            CONTROL = 14,
            MAX = 19,
            UNKNOWN = 0
        };
        public enum StreamDirection{
            IN,
            OUT
        };
        public OracleTransaction(DebugLogger logger)
            : base(logger)
        {
            redirPort = -1;
        }
        public override string Name() {
            return "Oracle Transaction Factory";
        }
        public override bool HandleSession(IPSession session) {
            //List<Configuration.Setting> settings = Configuration.ParserConfiguration.Settings[Name()];
            //foreach(Configuration.Setting setting in settings)
            //    logger.AddMessage("Using property {0} with value {1}", setting.Property, setting.Value);
            PacketStream stream = session.GetNextStreamDirection();
            if ((session.RemoteEndpoint.Port == 1521) || (session.RemoteEndpoint.Port == redirPort)) {
                //we're either connected to the standard Oracle port or the redirected port
            }else
                return false;
            while (true) {
                try {
                    TransactionNode transaction = new TransactionNode("OracleTransaction");
                    transaction.Description = transaction.Name;
                    TransactionNode tnsNode = ExtractTNSData(stream, StreamDirection.OUT);
                    if(tnsNode != null)
                        transaction.AddChild(tnsNode);  
                    //response stream
                    stream = session.GetNextStreamDirection();
                    if (stream.GetBytesAvailable() != 0) {
                        tnsNode = ExtractTNSData(stream, StreamDirection.IN);

                        if(tnsNode != null)
                            transaction.AddChild(tnsNode);
                    }
                    if(transaction.Children.Count > 0)
                        session.AddNode(transaction);
                    //request stream (if exists)
                    stream = session.GetNextStreamDirection();

                    if(stream.GetBytesAvailable() == 0)
                        break;
                } catch (EndOfStreamException) {
                    break;
                }

            }
            return true;
        }

        private TransactionNode ExtractTNSData(PacketStream stream, StreamDirection direction) {
            TransactionNode node;
            OracleTransactionType type;
            List<PacketSlice> slices = new List<PacketSlice>();
            Int16 pLen = 0;
            int packetType;
            try {
                
                pLen = stream.PeekInt16();
                stream.ReadBytes(2, slices);
                stream.ReadBytes(2); //skip checksum
                packetType = stream.ReadByte();
                type = (OracleTransactionType)packetType;
                stream.ReadByte(); //skip the reserved byte
            } catch (Exception e) {
                logger.AddMessage(e.Message);
                return null;
            }
            switch (type) {
                case OracleTransactionType.CONNECT:
                    try {
                        node = new TransactionNode("Connect");
                        node.AddField("Packet Size", pLen, "Packet Size", slices);

                        Int16 headerChecksum = stream.ReadInt16();
                        Int16 version = stream.PeekInt16();
                        stream.ReadBytes(2, slices);
                        node.AddField("Version", version, "Version", slices);
                        Int16 compatVersion = stream.PeekInt16();
                        stream.ReadBytes(2, slices);
                        node.AddField("Version (compatible)", compatVersion, "Compatible Version", slices);
                        Int16 serviceOptions = stream.ReadInt16();
                        Int16 sessionDataUnitSize = stream.ReadInt16();
                        Int16 maxTxDataUnitSize = stream.ReadInt16();
                        Int16 NTProtCharacteristics = stream.ReadInt16();
                        Int16 lineTurnValue = stream.ReadInt16();
                        Int16 valueOfOneInHW = stream.ReadInt16();
                        Int16 ctDataLen = stream.PeekInt16();
                        stream.ReadBytes(2, slices);
                        node.AddField("Data Length", ctDataLen, "The length of the connect datastring.", slices);
                        Int16 ctDataOffset = stream.ReadInt16();
                        Int16 maxRecCtData = stream.ReadInt16();
                        Int16 ctFlagsA = stream.ReadInt16();
                        Int16 ctFlagsB = stream.ReadInt16();
                        Int32 traceItemA = stream.ReadInt32();
                        Int32 traceItemB = stream.ReadInt32();
                        Int64 traceID = stream.ReadInt64();
                        stream.ReadInt64();
                        byte[] ctData = stream.PeekBytes(ctDataLen);
                        string connectData = StaticUtils.DecodeASCII(ctData);

                        stream.ReadBytes(ctDataLen, slices);
                        node.AddField("Connect data", connectData, "Connect data", slices);
                        try {
                            Match match = Regex.Match(connectData, "\\(PROTOCOL=(?<proto>\\w{2,})\\)\\(HOST=(?<host>[.\\w]{7,})\\)\\(PORT=(?<port>\\d{2,})\\)\\).*SERVICE_NAME=(?<sid>[.\\w]{1,})\\)");
                            string proto = match.Groups["proto"].Value;
                            string host = match.Groups["host"].Value;
                            string newPort = match.Groups["port"].Value;
                            string sid = match.Groups["sid"].Value;
                            TransactionNode cInfoNode = new TransactionNode("Connection Info");
                            cInfoNode.AddField("Protocol", proto, "Protocol", slices);
                            cInfoNode.AddField("Host", host, "Server being connected to", slices);
                            cInfoNode.AddField("Port", newPort, "Port", slices);
                            cInfoNode.AddField("Service", sid, "Service", slices);
                            node.AddChild(cInfoNode);
                        } catch (ArgumentException) {
                        }
                        return node;
                    } catch (Exception e) {
                        logger.AddMessage(e.Message);
                        return null;
                    }
                case OracleTransactionType.ACCEPT:
                    node = new TransactionNode("Accept");
                    node.AddField("Packet Size", pLen, "Packet Size", slices);

                    try {
                        Int16 headerChecksum = stream.ReadInt16();
                        Int16 version = stream.PeekInt16();
                        stream.ReadBytes(2, slices);
                        node.AddField("Version", version, "Version", slices);
                        Int16 serviceOptions = stream.ReadInt16();
                        Int16 sessionDataUnitSize = stream.PeekInt16();
                        stream.ReadBytes(2, slices);
                        node.AddField("Session Data Unit Size", sessionDataUnitSize, "Session Data Unit Size", slices);
                        Int16 maxTxDataUnitSize = stream.PeekInt16();
                        stream.ReadBytes(2, slices);
                        node.AddField("Max Tx Unit Size", maxTxDataUnitSize, "Maximum Transmission Data Unit Size", slices);

                        Int16 valueOfOneInHW = stream.ReadInt16();
                        Int16 acceptDataLength = stream.ReadInt16();
                        Int16 dataOffset = stream.ReadInt16();
                        int connectFlagsA = stream.ReadByte();
                        int connectFlagsB = stream.ReadByte();
                        stream.ReadBytes(pLen - 24); //read out empty bytes
                    } catch (Exception e) {
                        logger.AddMessage(e.Message);
                        return null;
                    }
                    return node;
                case OracleTransactionType.REDIRECT:
                    node = new TransactionNode("Redirect");
                    node.AddField("Packet Size", pLen, "Packet Size", slices);

                    try {
                        Int16 headerChecksum = stream.ReadInt16();
                        Int16 redirLen = stream.PeekInt16();
                        stream.ReadBytes(2, slices);
                        node.AddField("Redirection Data Length", redirLen, "Length of the redirection data string", slices);
                        byte[] redirData = stream.PeekBytes(redirLen);
                        string sRedir = StaticUtils.DecodeASCII(redirData);
                        stream.ReadBytes(redirLen, slices);
                        node.AddField("Redirection Data", sRedir, "Redirection data", slices);
                        //get the redirected port
                        string newPort = null;
                        string proto = null;
                        string host = null;
                        try {
                            Match match = Regex.Match(sRedir, "\\(PROTOCOL=(?<proto>\\w{2,})\\)\\(HOST=(?<host>[.\\w]{7,})\\)\\(PORT=(?<port>\\d{2,})\\)\\)");
                            proto = match.Groups["proto"].Value;
                            host = match.Groups["host"].Value;
                            newPort = match.Groups["port"].Value;
                        } catch (ArgumentException) {
                            // Syntax error in the regular expression
                        }
                        if(!newPort.Equals(""))
                            redirPort = Int32.Parse(newPort);
                        TransactionNode redirInfo = new TransactionNode("Redirection Info");
                        redirInfo.AddField("Protocol", proto, "Protocol used", slices);
                        redirInfo.AddField("Host", host, "Host", slices);
                        redirInfo.AddField("Port", newPort, "Port", slices);
                        node.AddChild(redirInfo);
                    } catch (Exception e) {
                        logger.AddMessage(e.Message);
                        return null;
                    }
                    return node;
                case OracleTransactionType.DATA:
                    string label;
                    if (direction == StreamDirection.IN)
                        label = "Response";
                    else
                        label = "Request";
                    node = new TransactionNode("Data - "+label );
                    node.AddField("Packet Size", pLen, "Packet Size", slices);

                    try {
                        Int16 headerChecksum = stream.ReadInt16();
                        Int16 dataFlags = stream.ReadInt16();
                        int payLoadLength = pLen - 10;
                        byte[] payLoad = stream.PeekBytes(payLoadLength);
                        string sPayload = StaticUtils.DecodeASCII(payLoad);
                        stream.ReadBytes(payLoadLength, slices);
                        node.AddField("Data", sPayload, "Data", slices);
                    } catch (Exception e) {
                        logger.AddMessage(e.Message);
                        return null;
                    }
                    return node;
                default:
                    logger.AddMessage(String.Format("Packet dissection not implemented [TransactionType={0}]", type));
                    return null;
            }
        }
    }
}
