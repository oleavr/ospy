//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace oSpy.SharpDumpLib
{
    public interface IProtocolParser
    {
        string Name
        {
            get;
        }

        bool HandleStream (Stream stream, List<ProtocolNode> resultNodes, List<Stream> resultStreams);
    }

    public class ProtocolNode
    {
        public class Field
        {
            private ProtocolNode node;
            public ProtocolNode Node
            {
                get { return node; }
            }

            private string name;
            public string Name
            {
                get { return name; }
            }

            private string value;
            public string Value
            {
                get { return value; }
            }

            private StreamSegment segment;
            public StreamSegment Segment
            {
                get { return segment; }
            }
 
            public Field (ProtocolNode node, string name, string value, StreamSegment segment)
            {
                this.node = node;
                this.name = name;
                this.value = value;
                this.segment = segment;
            }
        }

        private Stream stream;
        public Stream Stream
        {
            get { return stream; }
        }

        private List<Field> fields = new List<Field> ();

        public Field AppendField (string name, string value, StreamSegment segment)
        {
            Field field = new Field (this, name, value, segment);
            fields.Add (field);
            return field;
        }
    }

    public class ProtocolReader
    {
        private Stream stream;

        public ProtocolReader (Stream stream)
        {
            this.stream = stream;
        }

        public StreamSegment CreateSegment ()
        {
            return new StreamSegment (stream);
        }
    }

    public class StreamSegment
    {
        private Stream stream;
        private long offset;
        private long size;

        public StreamSegment (Stream stream)
        {
            this.stream = stream;
            this.offset = stream.Position;
        }

        public string TryReadLine ()
        {
            return String.Empty;
        }

        public string ReadLine ()
        {
            string result = TryReadLine ();
            if (result == null)
                throw new Exception ("Failed to read line");
            return result;
        }
    }
}
