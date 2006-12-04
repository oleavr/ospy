/**
 * Copyright (C) 2006  Frode Hus <husfro@gmail.com>
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
