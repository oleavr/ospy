//
// Copyright (c) 2006-2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Flobbster.Windows.Forms;
using oSpy.Util;
using oSpy.Net;

namespace oSpy.Parser
{
    [Serializable()]
    public class HTTPMessage : VisualTransaction
    {
        public HTTPMessage(int index, PacketDirection direction, DateTime startTime)
            : base(index, direction, startTime, startTime)
        {
            HeaderRowsPerCol = 1000;
        }
    }

    public class HTTPVisualizer : SessionVisualizer
    {
        public override string Name
        {
            get { return "HTTP"; }
        }

        public override VisualTransaction[] GetTransactions(IPSession session)
        {
            List<HTTPMessage> messages = new List<HTTPMessage>();

            foreach (TransactionNode node in session.Nodes)
            {
                if (node.Name == "HTTPTransaction")
                {
                    TransactionNode subNode;

                    subNode = node.FindChild("Request", false);
                    if (subNode != null)
                        messages.Add(ParseNode(subNode, true));

                    subNode = node.FindChild("Response", false);
                    if (subNode != null)
                        messages.Add(ParseNode(subNode, false));

                    subNode = node.FindChild("Response2", false);
                    if (subNode != null)
                        messages.Add(ParseNode(subNode, false));
                }
            }

            return messages.ToArray();
        }

        private HTTPMessage ParseNode(TransactionNode httpNode, bool isRequest)
        {
            IPPacket pkt = httpNode.Slices[0].Packet;

            HTTPMessage msg = new HTTPMessage(httpNode.Index, pkt.Direction, pkt.Timestamp);

            if (isRequest)
            {
                msg.HeadlineText = String.Format("{0} {1} {2}", httpNode["Verb"], httpNode["Argument"], httpNode["Protocol"]);
            }
            else
            {
                msg.HeadlineText = String.Format("{0} {1}", httpNode["Protocol"], httpNode["Result"]);
            }

            TransactionNode headersNode = httpNode.FindChild("Headers", false);
            if (headersNode != null)
            {
                foreach (string name in headersNode.FieldNames)
                {
                    msg.AddHeaderField(name, headersNode.Fields[name]);
                }
            }

            TransactionNode bodyNode = httpNode.FindChild("Body", false);
            if (bodyNode != null)
            {
                if (bodyNode.Fields.ContainsKey("XML"))
                {
                    string body;
                    XmlHighlighter highlighter = new XmlHighlighter(XmlHighlightColorScheme.VisualizationScheme);

                    XmlUtils.PrettyPrint((string)bodyNode["XML"], out body, highlighter);

                    msg.BodyText = body;

                    highlighter.HighlightRichTextBox(msg.BodyBox);
                }
                else if (bodyNode.Fields.ContainsKey("HTML"))
                {
                    msg.BodyText = (string)bodyNode["HTML"];
                }
                else if (bodyNode.Fields.ContainsKey("Raw"))
                {
                    msg.SetBodyFromPreviewData((byte[])bodyNode.Fields["Raw"], 512);
                }
                else
                {
                    msg.BodyText = String.Format("{0} unhandled", bodyNode.FieldNames[0]);
                }
            }

            return msg;
        }
    }

    public class HTTPTransactionFactory : TransactionFactory
    {
        protected const string ellipsis = "(..)";
        protected const int MAX_DESCRIPTION_LENGTH = 40;

        private enum HTTPTransactionType
        {
            REQUEST,
            RESPONSE,
        };

        public HTTPTransactionFactory(DebugLogger logger)
            : base(logger)
        {
        }

        public override string Name() {
             return "HTTP Transaction Factory";
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

                    TransactionNode request = ExtractHttpData(stream, HTTPTransactionType.REQUEST);
                    transaction.AddChild(request);

                    string desc = request.Description;

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

                        desc += " => " + response.Description;
                    }

                    transaction.Description = desc;

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
                stream.ReadBytes(StaticUtils.GetUTF8ByteCount(tokens[0]), slices);
                node.AddField("Verb", tokens[0], "Request verb.", slices);

                stream.ReadByte();

                stream.ReadBytes(StaticUtils.GetUTF8ByteCount(tokens[1]), slices);
                node.AddField("Argument", tokens[1], "Request argument.", slices);

                stream.ReadByte();

                stream.ReadBytes(StaticUtils.GetUTF8ByteCount(tokens[2]), slices);
                node.AddField("Protocol", tokens[2], "Protocol identifier.", slices);

                // Set a description
                node.Description = String.Format("{0} {1}", tokens[0], tokens[1]);
                if (node.Description.Length > MAX_DESCRIPTION_LENGTH)
                {
                    node.Description = node.Description.Substring(0, MAX_DESCRIPTION_LENGTH - ellipsis.Length);
                    node.Description += ellipsis;
                }
            }
            else
            {
                stream.ReadBytes(StaticUtils.GetUTF8ByteCount(tokens[0]), slices);
                node.AddField("Protocol", tokens[0], "Protocol identifier.", slices);

                if (tokens.Length > 1)
                {
                    stream.ReadByte();

                    stream.ReadBytes(StaticUtils.GetUTF8ByteCount(tokens[1]), slices);
                    node.AddField("Result", tokens[1], "Result.", slices);
                }

                // Set a description
                string result = tokens[1];
                if (result.Length > MAX_DESCRIPTION_LENGTH)
                {
                    result = result.Substring(0, MAX_DESCRIPTION_LENGTH - ellipsis.Length) + ellipsis;
                }

                node.Description = result;
            }

            stream.ReadBytes(2);

            TransactionNode headersNode = new TransactionNode("Headers");
            Dictionary<string, string> headerFields = new Dictionary<string, string>();

            do
            {
                line = stream.PeekLineUTF8();
                if (line.Length > 0)
                {
                    tokens = line.Split(new char[] { ':' }, 2);
                    if (tokens.Length < 2)
                        throw new ProtocolError();

                    string key = tokens[0];
                    string value = tokens[1].TrimStart();

                    stream.ReadBytes(StaticUtils.GetUTF8ByteCount(line), slices);
                    headersNode.AddField(key, value, "Header field.", slices);

                    headerFields[key.ToLower()] = value;
                }

                stream.ReadBytes(2);
            } while (line != "");

            if (headersNode.Fields.Count > 0)
            {
                node.AddChild(headersNode);
            }

            int contentLen = -1;

            if (headerFields.ContainsKey("content-length"))
                contentLen = Convert.ToInt32(headerFields["content-length"]);
            else if (headerFields.ContainsKey("contentlength"))
                contentLen = Convert.ToInt32(headerFields["contentlength"]);

            if (contentLen != -1)
            {
                string contentType = null, contentEncoding = null;

                if (headerFields.ContainsKey("content-type"))
                    contentType = headerFields["content-type"];
                else if (headerFields.ContainsKey("contenttype"))
                    contentType = headerFields["contenttype"];
 
                if (contentType != null)
                {
                    contentType = contentType.ToLower();

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
                bodyNode.AddField("Raw", body, StaticUtils.FormatByteArray(body),
                    "Raw body data.", slices);
            }

            transactionNode.AddChild(bodyNode);
        }
    }
}
