/**
 * Copyright (C) 2007  Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Collections;
using System.Runtime.Serialization;

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
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, ConfigContext>));
            contexts = (Dictionary<string, ConfigContext>) serializer.ReadObject(stream);
            stream.Close();
        }

        public static void Save()
        {
            List<ConfigContext> contextList = new List<ConfigContext>(contexts.Values);

            Stream stream = File.Open(GetConfigFilePath(), FileMode.Create);
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, ConfigContext>));
            serializer.WriteObject(stream, contexts);
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
