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

using System;
using System.Text;
using System.Xml;
using oSpyStudio.Widgets;
using oSpy.SharpDumpLib;

public class EventNodeRenderer : INodeRenderer
{
    public Gtk.Widget Render(INode node)
    {        
        Gtk.Button button = new Gtk.Button();

        Event ev = (node as EventNode).Event;

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(ev.RawData);

        string fullFunctionName = doc.DocumentElement.SelectSingleNode("/event/name").InnerText.Trim();
        string functionName = fullFunctionName.Split(new string[] { "::" }, StringSplitOptions.None)[1];

        button.Label = functionName + "()";

        if (ev is IDataTransfer)
        {
            IDataTransfer transfer = ev as IDataTransfer;

            byte[] data = null;

            switch (transfer.Direction)
            {
                case DataTransferDirection.Incoming:
                    data = transfer.IncomingData;
                    break;
                
                case DataTransferDirection.Outgoing:
                    data = transfer.OutgoingData;
                    break;

                case DataTransferDirection.Both:
                    data = transfer.IncomingData;
                    break;
            }

            try
            {
                string preview = Encoding.UTF8.GetString(data);
                if (preview.Length > 32)
                    button.Label += ": " + preview.Substring(0, 32);
                else
                    button.Label += ": " + preview;
            }
            catch (Exception)
            {
            }
        }

        button.SetSizeRequest((int) node.Allocation.Width, (int) node.Allocation.Height);
        return button;
    }
}