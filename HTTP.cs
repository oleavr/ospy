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

namespace oSpy
{
    public class HTTPTransactionFactory : TransactionFactory
    {
        private enum HTTPTransactionType
        {
            REQUEST,
            RESPONSE,
        };

        public HTTPTransactionFactory(DebugLogger logger)
            : base(logger)
        {
        }

        public override bool HandleSession(IPSession session)
        {
            PacketStream stream = session.GetNextStreamDirection();
            string line;

            try
            {
                line = stream.PeekLineUTF8();
            }
            catch (EndOfStreamException)
            {
                return false;
            }

            string[] tokens = line.Split(new char[] { ' ' });
            if (!tokens[tokens.Length - 1].StartsWith("HTTP/1."))
            {
                return false;
            }

            // At this point it should be safe enough to assume we're
            // dealing with an HTTP session.

            while (true)
            {
                try
                {
                    TransactionNode transaction = new TransactionNode("HTTPTransaction");
                    transaction.Description = transaction.Name;

                    TransactionNode request = ExtractHttpData(stream, HTTPTransactionType.REQUEST);
                    transaction.AddChild(request);

                    stream = session.GetNextStreamDirection();
                    if (stream.GetBytesAvailable() != 0)
                    {
                        TransactionNode response = ExtractHttpData(stream, HTTPTransactionType.RESPONSE);
                        transaction.AddChild(response);

                        if (response.Fields.ContainsKey("Result") &&
                            ((string)response.Fields["Result"]).StartsWith("100 "))
                        {
                            response = ExtractHttpData(stream, HTTPTransactionType.RESPONSE, "Response2");
                            transaction.AddChild(response);
                        }
                    }

                    session.AddNode(transaction);

                    stream = session.GetNextStreamDirection();
                    if (stream.GetBytesAvailable() == 0)
                        break;
                }
                catch (EndOfStreamException)
                {
                    logger.AddMessage("HTTP premature EOF");
                    break;
                }
                catch (ProtocolError)
                {
                    logger.AddMessage("HTTP protocol error");
                    break;
                }
            }

            return true;
        }

        private TransactionNode ExtractHttpData(PacketStream stream, HTTPTransactionType type)
        {
            return ExtractHttpData(stream, type, null);
        }

        private TransactionNode ExtractHttpData(PacketStream stream, HTTPTransactionType type, string nodeName)
        {
            if (nodeName == null)
            {
                nodeName = (type == HTTPTransactionType.REQUEST) ? "Request" : "Response";
            }

            TransactionNode node = new TransactionNode(nodeName);
            List<PacketSlice> slices = new List<PacketSlice>();

            string line = stream.PeekLineUTF8();

            int fieldCount = (type == HTTPTransactionType.REQUEST) ? 3 : 2;

            string[] tokens = line.Split(new char[] { ' ' }, fieldCount);
            if (tokens.Length < fieldCount)
                throw new ProtocolError();

            if (type == HTTPTransactionType.REQUEST)
            {
                stream.ReadBytes(Util.GetUTF8ByteCount(tokens[0]), slices);
                node.AddField("Verb", tokens[0], "Request verb.", slices);

                stream.ReadByte();

                stream.ReadBytes(Util.GetUTF8ByteCount(tokens[1]), slices);
                node.AddField("Argument", tokens[1], "Request argument.", slices);

                stream.ReadByte();

                stream.ReadBytes(Util.GetUTF8ByteCount(tokens[2]), slices);
                node.AddField("Protocol", tokens[2], "Protocol identifier.", slices);
            }
            else
            {
                stream.ReadBytes(Util.GetUTF8ByteCount(tokens[0]), slices);
                node.AddField("Protocol", tokens[0], "Protocol identifier.", slices);

                if (tokens.Length > 1)
                {
                    stream.ReadByte();

                    stream.ReadBytes(Util.GetUTF8ByteCount(tokens[1]), slices);
                    node.AddField("Result", tokens[1], "Result.", slices);
                }
            }

            stream.ReadBytes(2);

            TransactionNode headersNode = new TransactionNode("Headers");

            do
            {
                line = stream.PeekLineUTF8();
                if (line.Length > 0)
                {
                    tokens = line.Split(new char[] { ':' }, 2);
                    if (tokens.Length < 2)
                        throw new ProtocolError();

                    stream.ReadBytes(Util.GetUTF8ByteCount(line), slices);
                    headersNode.AddField(tokens[0], tokens[1].TrimStart(), "Header field.", slices);
                }

                stream.ReadBytes(2);
            } while (line != "");

            if (headersNode.Fields.Count > 0)
            {
                node.AddChild(headersNode);
            }

            if (headersNode.Fields.ContainsKey("Content-Length"))
            {
                string contentType = null, contentEncoding = null;

                if (headersNode.Fields.ContainsKey("Content-Type"))
                {
                    contentType = ((string)headersNode.Fields["Content-Type"]).ToLower();
                    tokens = contentType.Split(new char[] { ';' });
                    contentType = tokens[0].Trim();
                    if (tokens.Length > 1)
                    {
                        string[] encTokens = tokens[1].Split(new char[] { '=' }, 2);
                        if (encTokens[0].Trim() == "charset" && encTokens.Length > 1)
                        {
                            contentEncoding = encTokens[1];
                        }
                    }
                }

                string str = stream.PeekStringASCII(5);
                if (str == "<?xml")
                {
                    contentType = "text/xml";
                    contentEncoding = "utf-8"; // FIXME
                }

                int contentLen = Convert.ToInt32(headersNode.Fields["Content-Length"]);
                if (contentLen > 0)
                {
                    AddBodyNode(stream, node, contentType, contentEncoding, contentLen);
                }
            }
            
            return node;
        }

        protected void AddBodyNode(PacketStream stream,
                                   TransactionNode transactionNode,
                                   string contentType,
                                   string contentEncoding,
                                   int bodyLen)
        {
            List<PacketSlice> slices = new List<PacketSlice>(1);

            TransactionNode bodyNode = new TransactionNode("Body");
            byte[] body = stream.ReadBytes(bodyLen, slices);

            if (contentType == "text/html" || contentType == "text/xml")
            {
                int realBodyLen = body.Length;
                if (body[realBodyLen - 1] == '\0')
                    realBodyLen--;

                Decoder dec;
                if (contentEncoding == "utf-8")
                {
                    dec = Encoding.UTF8.GetDecoder();
                }
                else
                {
                    dec = Encoding.ASCII.GetDecoder();
                }

                char[] bodyChars = new char[dec.GetCharCount(body, 0, realBodyLen)];
                dec.GetChars(body, 0, realBodyLen, bodyChars, 0);
                string bodyStr = new string(bodyChars);

                if (contentType == "text/xml")
                    bodyNode.AddXMLField("XML", bodyStr, "Body XML data.", slices);
                else
                    bodyNode.AddTextField("HTML", bodyStr, "Body HTML data.", slices);
            }
            else if (contentType == "application/vnd.ms-sync.wbxml")
            {
                string xml = WBXML.ConvertToXML(body);
                bodyNode.AddXMLField("WBXML", xml, "Body WBXML data.", slices);
            }
            else
            {
                bodyNode.AddField("Raw", Util.FormatByteArray(body),
                    "Raw body data.", slices);
            }

            transactionNode.AddChild(bodyNode);
        }
    }
}
