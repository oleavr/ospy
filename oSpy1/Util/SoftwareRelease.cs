/**
 * Copyright (C) 2009  Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Xml;

namespace oSpy.Util
{
    class SoftwareRelease
    {
        private Version version;
        public Version Version
        {
            get { return version; }
        }

        private string url;
        public string Url
        {
            get { return url; }
        }

        protected SoftwareRelease (Version version, string url)
        {
            this.version = version;
            this.url = url;
        }

        private const string versionXmlUrl = "http://ospy.googlecode.com/files/oSpyVersion.xml";

        public static SoftwareRelease GetLatest ()
        {
            SoftwareRelease latest = null;

            try
            {
                XmlTextReader reader = new XmlTextReader (versionXmlUrl);
                reader.MoveToContent ();

                Version version = null;
                string url = null;

                string elementName = "";

                if ((reader.NodeType == XmlNodeType.Element) &&
                    (reader.Name == "ospy"))
                {
                    while (reader.Read ())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                            elementName = reader.Name;
                        else if ((reader.NodeType == XmlNodeType.Text) &&
                            (reader.HasValue))
                        {
                            switch (elementName)
                            {
                                case "version":
                                    version = new Version (reader.Value);
                                    break;
                                case "url":
                                    url = reader.Value;
                                    break;
                            }
                        }
                    }
                }

                reader.Close ();

                if (version != null && url != null)
                {
                    latest = new SoftwareRelease (version, url);
                }
            }
            catch (Exception)
            {
            }

            return latest;
        }
    }
}
