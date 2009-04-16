//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
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

namespace oSpy.SharpDumpLib.Tests
{
    public class TestEventXml
    {        
        public static string E001_Error
        {
            get
            {
                return "<event id=\"1\" type=\"Error\" timestamp=\"128837553502326832\" processName=\"msnmsgr.exe\" processId=\"2684\" threadId=\"1128\">"
                      +    "<message>"
                      +        "signature 'RTCDebug' specified for function not found: No matches found"
                      +    "</message>"
                      +"</event>";
            }
        }

        public static string E083_CreateSocket
        {
            get
            {
                return "<event id=\"83\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553521454336\" type=\"FunctionCall\">"
                      +    "<name>"
                      +        "WS2_32.dll::socket"
                      +    "</name>"
                      +    "<arguments direction=\"in\">"
                      +        "<argument name=\"af\">"
                      +            "<value subType=\"SockAddrFamily\" type=\"Enum\" value=\"AF_INET\"/>"
                      +        "</argument>"
                      +        "<argument name=\"type\">"
                      +            "<value subType=\"SockType\" type=\"Enum\" value=\"SOCK_STREAM\"/>"
                      +        "</argument>"
                      +        "<argument name=\"protocol\">"
                      +            "<value subType=\"AF_INET_Protocol\" type=\"Dynamic\" value=\"IPPROTO_IP\"/>"
                      +        "</argument>"
                      +    "</arguments>"
                      +    "<returnValue>"
                      +        "<value type=\"UInt32\" value=\"0x8ac\"/>"
                      +    "</returnValue>"
                      +    "<lastError value=\"0\"/>"
                      +"</event>";
            }
        }

        public static string E084_Connect
        {
            get
            {
                return "<event id=\"84\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553521454336\" type=\"FunctionCall\">"
                      +    "<name>"
                      +        "WS2_32.dll::connect"
                      +    "</name>"
                      +    "<arguments direction=\"in\">"
                      +        "<argument name=\"s\">"
                      +            "<value type=\"UInt32\" value=\"0x8ac\"/>"
                      +        "</argument>"
                      +        "<argument name=\"name\">"
                      +            "<value type=\"Ipv4SockaddrPtr\" value=\"0x0006FCEC\">"
                      +                "<value subType=\"Ipv4Sockaddr\" type=\"Struct\">"
                      +                    "<field name=\"sin_family\">"
                      +                        "<value subType=\"SockAddrFamily\" type=\"Enum\" value=\"AF_INET\"/>"
                      +                    "</field>"
                      +                    "<field name=\"sin_port\">"
                      +                        "<value type=\"UInt16\" value=\"1863\"/>"
                      +                    "</field>"
                      +                    "<field name=\"sin_addr\">"
                      +                        "<value type=\"Ipv4InAddr\" value=\"65.54.239.20\"/>"
                      +                    "</field>"
                      +                "</value>"
                      +            "</value>"
                      +        "</argument>"
                      +        "<argument name=\"namelen\">"
                      +            "<value type=\"Int32\" value=\"16\"/>"
                      +        "</argument>"
                      +    "</arguments>"
                      +    "<returnValue>"
                      +        "<value type=\"Int32\" value=\"-1\"/>"
                      +    "</returnValue>"
                      +    "<lastError value=\"10035\"/>"
                      +"</event>";
            }
        }

        public static string E096_Send
        {
            get
            {
                return "<event id=\"96\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553523557360\" type=\"FunctionCall\">"
                      +    "<name>"
                      +        "WS2_32.dll::send"
                      +    "</name>"
                      +    "<arguments direction=\"in\">"
                      +        "<argument name=\"s\">"
                      +            "<value type=\"UInt32\" value=\"0x8ac\"/>"
                      +        "</argument>"
                      +        "<argument name=\"buf\">"
                      +            "<value type=\"Pointer\" value=\"0x020DCD38\">"
                      +                "<value size=\"26\" type=\"ByteArray\">"
                      +                    "VkVSIDEgTVNOUDE4IE1TTlAxNyBDVlIwDQo="
                      +                "</value>"
                      +            "</value>"
                      +        "</argument>"
                      +        "<argument name=\"len\">"
                      +            "<value type=\"Int32\" value=\"26\"/>"
                      +        "</argument>"
                      +        "<argument name=\"flags\">"
                      +            "<value type=\"Int32\" value=\"0\"/>"
                      +        "</argument>"
                      +    "</arguments>"
                      +    "<returnValue>"
                      +        "<value type=\"Int32\" value=\"26\"/>"
                      +    "</returnValue>"
                      +    "<lastError value=\"0\"/>"
                      +"</event>";
            }
        }

        public static string E130_Receive
        {
            get
            {
                return "<event id=\"130\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553525259808\" type=\"FunctionCall\">"
                      +    "<name>"
                      +        "WSOCK32.dll::recv"
                      +    "</name>"
                      +    "<arguments direction=\"in\">"
                      +        "<argument name=\"s\">"
                      +            "<value type=\"UInt32\" value=\"0x8ac\"/>"
                      +        "</argument>"
                      +        "<argument name=\"buf\">"
                      +            "<value type=\"Pointer\" value=\"0x00C08230\"/>"
                      +        "</argument>"
                      +        "<argument name=\"len\">"
                      +            "<value type=\"Int32\" value=\"512\"/>"
                      +        "</argument>"
                      +        "<argument name=\"flags\">"
                      +            "<value type=\"Int32\" value=\"0\"/>"
                      +        "</argument>"
                      +    "</arguments>"
                      +    "<arguments direction=\"out\">"
                      +        "<argument name=\"buf\">"
                      +            "<value type=\"Pointer\" value=\"0x00C08230\">"
                      +                "<value size=\"14\" type=\"ByteArray\">"
                      +                    "VkVSIDEgTVNOUDE4DQo="
                      +                "</value>"
                      +            "</value>"
                      +        "</argument>"
                      +    "</arguments>"
                      +    "<returnValue>"
                      +        "<value type=\"Int32\" value=\"14\"/>"
                      +    "</returnValue>"
                      +    "<lastError value=\"0\"/>"
                      +"</event>";
            }
        }

        public static string E140_CloseSocket
        {
            get
            {
                return "<event id=\"140\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553527062400\" type=\"FunctionCall\">"
                      +    "<name>"
                      +        "WS2_32.dll::closesocket"
                      +    "</name>"
                      +    "<arguments direction=\"in\">"
                      +        "<argument name=\"s\">"
                      +            "<value type=\"UInt32\" value=\"0x8ac\"/>"
                      +        "</argument>"
                      +    "</arguments>"
                      +    "<returnValue>"
                      +        "<value type=\"Int32\" value=\"0\"/>"
                      +    "</returnValue>"
                      +    "<lastError value=\"0\"/>"
                      +"</event>";
            }
        }
    }
}
