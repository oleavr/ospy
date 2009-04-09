using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NUnit.Framework;
using ICSharpCode.SharpZipLib.BZip2;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture]
    public class AnalysisTests
    {
#if false
        [Test]
        public void TestKsIoCtl ()
        {
            BZip2InputStream stream = new BZip2InputStream (File.OpenRead (@"..\..\lowlightboost_min_to_max.osd"));
            DumpLoader loader = new DumpLoader ();
            Dump dump = loader.Load (stream);

            Event ev = dump.Events[2];

            KsIoCtlParser parser = new KsIoCtlParser ();
            bool handled = parser.ParseEvent (ev);
            Assert.IsTrue (handled);

            Node node = ev.Node;
            Assert.AreEqual ("IOCTL_KS_PROPERTY", node.Name);

            Node inputNode = node["Input"];
            Assert.AreEqual ("KSPROPSETID_Topology", inputNode["Set"]); // 720D4AC0-7533-11D0-A5D6-28DB04C10000
            Assert.AreEqual ("KSPROPERTY_TOPOLOGY_CATEGORIES", inputNode["Id"]);
            Assert.AreEqual ("KSPROPERTY_TYPE_GET", inputNode["Flags"]);

            Node outputNode = node["Output"];
            Assert.AreEqual ("38 00 00 00 03 00 00 00 05 AD 94 69 EF 93 D0 11 A3 CC 00 A0 C9 22 31 96 3D 77 E8 65 56 8F D0 11 A3 B9 00 A0 C9 22 31 96 8A 42 6C FB 53 03 D1 11 90 5F 00 00 C0 CC 16 BA",
                outputNode["Contents"]);
        }
#endif

        //[Test]
        public void TestFooBar ()
        {
            Console.WriteLine ("Loading");
            DumpLoader loader = new DumpLoader ();
            Dump dump = loader.Load (new BZip2InputStream (File.OpenRead (@"C:\Projects\oSpy\oSpy.SharpDumpLib\Tests\test.osd")));

            Console.WriteLine ("Parsing");
            DumpParser parser = new DumpParser ();
            List<Process> processes = parser.Parse (dump);

            //Stream stream = File.Create ("test.bin");
            //BinaryFormatter formatter = new BinaryFormatter ();
            //XmlSerializer serializer = new XmlSerializer (typeof (MemoryDataTransfer));
            //XmlTextWriter writer = new XmlTextWriter ("test.xml", Encoding.UTF8);

            foreach (Process process in processes)
            {
                foreach (Resource resource in process.Resources)
                {
                    foreach (DataTransfer transfer in resource.DataTransfers)
                    {
                        if (transfer.Size > 100)
                        {
                            MemoryDataTransfer memTransfer = new MemoryDataTransfer (transfer);

                            //formatter.Serialize (stream, memTransfer);
                            //serializer.Serialize (writer, memTransfer);

                            //stream.Close ();

                            XmlTextWriter writer = new XmlTextWriter (Console.Out);
                            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer (memTransfer.GetType ());
                            x.Serialize (writer, memTransfer);
                            Console.WriteLine ();
                            return;
                        }
                    }
                }
            }
        }
    }
}
