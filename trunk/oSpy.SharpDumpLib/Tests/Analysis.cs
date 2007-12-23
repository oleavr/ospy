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
        [Test]
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
