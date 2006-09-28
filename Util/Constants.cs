/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
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
