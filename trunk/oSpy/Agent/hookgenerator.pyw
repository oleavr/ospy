# -*- coding: utf-8 -*-
#
# Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
#
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
# 
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
#

import win32clipboard as clip
import win32con

def parse_signature(sig):    
    tokens = sig.replace("\r\n", "").replace("\n", "").split("(", 2)

    subtokens = tokens[0].split(" ")

    ret_type = subtokens[0]
    fname = subtokens[-1]

    args = tokens[1].rstrip(";").rstrip(")").split(",")
    args = [a.strip() for a in args]

    return (ret_type, fname, args)

TEMPLATE_CALLED = 1
TEMPLATE_DONE   = 2

def generate_template(sig, type):
    ret_type, fname, args = parse_signature(sig)

    if type == TEMPLATE_CALLED:
        fn_prefix = "%s_called(" % fname
        conv = "__cdecl"
    else:
        fn_prefix = "%s_done(" % fname
        conv = "__stdcall"

    fwidth = len(fn_prefix)
    
    s = "static %s %s\r\n%s" % (ret_type, conv, fn_prefix)

    if type == TEMPLATE_CALLED:
        s += "BOOL carry_on,\r\n%*sDWORD ret_addr" % (fwidth, "",)
    else:
        s += "%s retval" % ret_type
    
    for arg in args:
        s += ",\r\n%*s%s" % (fwidth, "", arg)

    s += ")\r\n{\r\n"

    if type == TEMPLATE_CALLED:
        if ret_type == "BOOL":
            s += "    return TRUE;\r\n"
        else:
            s += "    return 0;\r\n"
    else:
        s += "    DWORD err = GetLastError();\r\n"
        s += "    int ret_addr = *((DWORD *) ((DWORD) &retval - 4));\r\n"
        s += "\r\n"
        s += "    SetLastError(err);\r\n"
        s += "    return retval;\r\n"

    s += "}\r\n"

    return s
        

clip.OpenClipboard()
sig = clip.GetClipboardData(win32con.CF_TEXT)

s = generate_template(sig, TEMPLATE_CALLED)
s += "\r\n"
s += generate_template(sig, TEMPLATE_DONE)

clip.EmptyClipboard()
clip.SetClipboardData(win32con.CF_TEXT, s)

clip.CloseClipboard()
