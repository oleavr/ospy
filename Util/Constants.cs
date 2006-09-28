using System;
using System.Collections.Generic;
using System.Text;

namespace oSpy.Util
{
    public class Constants
    {
        public const int ERROR_SUCCESS = 0;

        public const uint REG_NONE = 0;
        public const uint REG_SZ = 1;
        public const uint REG_EXPAND_SZ = 2;
        public const uint REG_BINARY = 3;
        public const uint REG_DWORD = 4;
        public const uint REG_DWORD_BIG_ENDIAN = 5;
        public const uint REG_LINK = 6;
        public const uint REG_MULTI_SZ = 7;
        public const uint REG_RESOURCE_LIST = 8;

        public const int REG_CREATED_NEW_KEY = 0x1;
        public const int REG_OPENED_EXISTING_KEY = 0x2;
    }
}
