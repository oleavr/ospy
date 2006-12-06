using System;
using System.Collections.Generic;
using System.Text;
using oSpy;
using System.IO;
using System.Diagnostics;
using oSpy.Parser;
using oSpy.Net;
namespace TestPacketStream
{
    class Program
    {
        static void Main(string[] args)
        {
            PacketStream stream = new PacketStream();

            IPEndpoint localEndpoint = new IPEndpoint("169.254.2.2", 27516);
            IPEndpoint remoteEndpoint = new IPEndpoint("169.254.2.1", 1056);

            int n = 1;

            IPPacket p = new IPPacket(n++, 1, PacketDirection.PACKET_DIRECTION_OUTGOING, localEndpoint, remoteEndpoint,
                                      NewArrayIncremental(6, 10));
            stream.AppendPacket(p);

            p = new IPPacket(n++, 1, PacketDirection.PACKET_DIRECTION_OUTGOING, localEndpoint, remoteEndpoint,
                             NewArrayIncremental(3, 35));
            stream.AppendPacket(p);

            p = new IPPacket(n++, 1, PacketDirection.PACKET_DIRECTION_OUTGOING, localEndpoint, remoteEndpoint,
                             NewArrayIncremental(1, 60));
            stream.AppendPacket(p);

            p = new IPPacket(n++, 1, PacketDirection.PACKET_DIRECTION_OUTGOING, localEndpoint, remoteEndpoint,
                             NewArrayIncremental(1, 200));
            stream.AppendPacket(p);

            p = new IPPacket(n++, 1, PacketDirection.PACKET_DIRECTION_OUTGOING, localEndpoint, remoteEndpoint,
                             NewArrayIncremental(3, 210));
            stream.AppendPacket(p);

            p = new IPPacket(n++, 1, PacketDirection.PACKET_DIRECTION_OUTGOING, localEndpoint, remoteEndpoint,
                             NewArrayIncremental(1, 70));
            stream.AppendPacket(p);

            p = new IPPacket(n++, 1, PacketDirection.PACKET_DIRECTION_OUTGOING, localEndpoint, remoteEndpoint,
                             NewArrayIncremental(1, 80));
            stream.AppendPacket(p);

            Debug.Assert(stream.Position == 0, "Position != 0");
            Debug.Assert(stream.Length == 16, "Length != 16");

            byte[] buf = new byte[stream.Length];
            Debug.Assert(stream.Read(buf, 0, 16) == 16, "Read() != 16");
            byte[] expectedBytes = new byte[] { 10, 11, 12, 13, 14, 15,
                                                35, 36, 37,
                                                60,
                                                200,
                                                210, 211, 212,
                                                70,
                                                80 };

            Debug.Assert(CompareByteArrays(buf, expectedBytes), "Content after first read is not as expected");

            Debug.Assert(stream.Position == 16, "Position != 16");

            Debug.Assert(stream.Read(buf, 0, 10) == 0, "Read at offset 16 doesn't return 0");

            stream.Seek(-1, SeekOrigin.Current);
            Debug.Assert(stream.Read(buf, 2, 100) == 1, "Read at offset 15 doesn't return 1");

            Debug.Assert(buf[2] == 80, "Byte at offset 15 isn't 80");

            stream.Seek(-16, SeekOrigin.Current);
            Debug.Assert(stream.Position == 0, "Position != 0 after reverse seek");

            stream.Position = 9;
            Debug.Assert(stream.Read(buf, 1, 1) == 1, "Read at Position=9 didn't yield 1 byte");
            Debug.Assert(buf[1] == 60, "buf[1] != 60");

            stream.Position = 13;
            Debug.Assert(stream.Read(buf, 5, 4) == 3, "Read at Position=13 didn't yield 3 bytes");
            Debug.Assert(buf[5] == 212, "buf[5] != 212");
            Debug.Assert(buf[6] == 70, "buf[5] != 70");
            Debug.Assert(buf[7] == 80, "buf[5] != 80");

            System.Console.WriteLine("All tests passed");
            System.Console.ReadKey();
        }

        private static void PrintByteArray(byte[] buf, int count)
        {
            System.Console.Write("Bytes: [");
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    System.Console.Write(",");
                System.Console.Write(" {0}", buf[i]);
            }
            System.Console.WriteLine("]");
        }

        public static bool CompareByteArrays(byte[] data1, byte[] data2)
        {
            // If both are null, they're equal
            if (data1 == null && data2 == null)
                return true;

            // If either but not both are null, they're not equal
            if (data1 == null || data2 == null)
                return false;

            if (data1.Length != data2.Length)
                return false;

            for (int i = 0; i < data1.Length; i++)
            {
                if (data1[i] != data2[i])
                    return false;
            }

            return true;
        }

        static private byte[] NewArrayIncremental(int size, int start)
        {
            byte[] bytes = new byte[size];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte) (start + i);
            }

            return bytes;
        }
    }
}
