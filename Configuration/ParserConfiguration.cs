using System;
using System.Collections.Generic;
using System.Text;

namespace oSpy.Configuration {
    public class ParserConfiguration {
        private static SortedList<string,List<Setting>> settings;
        public ParserConfiguration(){

        }
        public static SortedList<string,List<Setting>> Settings{
            get{
                if(settings == null)
                    settings = new SortedList<string,List<Setting>>();
                return settings;
            }
        }
    }

    public class Setting {
        private string property;
        private string propValue;
        public string Property {
            get {
                return property;
            }
            set {
                property = value;
            }
        }
        public string Value {
            get {
                return propValue;
            }
            set {
                propValue = value;
            }
        }
    }
}
