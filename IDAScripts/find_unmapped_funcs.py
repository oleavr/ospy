# -*- coding: utf-8 -*-
#
# Copyright (C) 2007  Ole André Vadla Ravnås <oleavr@gmail.com>
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

import idaapi
from idautils import *
import util

def find_unmapped_funcs():
    funcs = {}

    i = 0
    ea = 0
    max_ea = 0xFFFFFFFF
    while True:
        ea = NextHead(ea, max_ea)
        if ea == BADADDR:
            break

        flags = idaapi.getFlags(ea)
        if idaapi.isCode(flags):
            if not idaapi.get_func(ea):
                s = util.scan_insn_for_debug_ref(ea)
                if s != None:
                    if not s in funcs:
                        funcs[s] = ea

    return funcs

if __name__ == "__main__":
    filename = AskFile(1, "*.txt", "Choose a filename for the result")
    if filename != None:
        print "Scanning for unmapped functions"
        funcs = find_unmapped_funcs()
        print "%d unmapped functions found" % len(funcs)

        sorted_funcs = funcs.keys()
        sorted_funcs.sort()

        f = open(filename, "w")
        for func in sorted_funcs:
            f.write("%s 0x%x\n" % (func, funcs[func]))
        f.close()

