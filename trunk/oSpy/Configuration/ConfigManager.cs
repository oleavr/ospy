//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
