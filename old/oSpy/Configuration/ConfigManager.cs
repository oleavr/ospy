//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace oSpy.Configuration
{
    public class ConfigManager
    {
        protected static Dictionary<string, ConfigContext> contexts;

        static ConfigManager()
        {
            contexts = new Dictionary<string, ConfigContext>();

            Load();
        }

        protected static string GetConfigFilePath()
        {
            string appDir =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);

            return String.Format("{0}\\config.dat", appDir);
        }

        public static void Load()
        {
            string path = GetConfigFilePath();

            if (!File.Exists(path))
                return;

            Stream stream = File.Open(path, FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();
            contexts = (Dictionary<string, ConfigContext>)bFormatter.Deserialize(stream);
            stream.Close();
        }

        public static void Save()
        {
            List<ConfigContext> contextList = new List<ConfigContext>(contexts.Values);

            Stream stream = File.Open(GetConfigFilePath(), FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, contexts);
            stream.Close();
        }

        public static ConfigContext GetContext(string name)
        {
            ConfigContext ctx;

            if (contexts.ContainsKey(name))
            {
                ctx = contexts[name];
            }
            else
            {
                ctx = new ConfigContext(name);
                contexts[name] = ctx;
            }

            return ctx;
        }
    }

    [Serializable()]
    public class ConfigContext
    {
        protected string name;
        public string Name
        {
            get { return name; }
        }

        protected Dictionary<string, object> settings;
        public object this[string name]
        {
            get { return settings[name]; }
            set { settings[name] = value; }
        }

        public ConfigContext(string name)
        {
            this.name = name;
            settings = new Dictionary<string, object>();
        }

        public bool HasSetting(string name)
        {
            return settings.ContainsKey(name);
        }
    }
}
