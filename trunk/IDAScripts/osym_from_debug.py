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

class DebugExtractor:
    def __init__(self, range):
        self._range = range

    def extract(self, filename):
        f = open(filename, "w")
        i = 0
        last_prog_update = 0

        f.write("%s\n" % GetInputFile())

        print "Retrieving functions"
        funcs = Functions(*self._range)

        print "Parsing functions"
        print "  0%%"
        for ea in funcs:
            chunks = []
            names = []

            func = idaapi.get_func(ea)
            iter = func_item_iterator_t(func)

            ea = iter.current()
            prev_chunknum = idaapi.get_func_chunknum(func, ea)
            cur_chunk = [ ea, ea ]
            chunks.append(cur_chunk)

            while True:
                ea = iter.current()
                cur_chunknum = idaapi.get_func_chunknum(func, ea)

                if cur_chunknum != prev_chunknum:
                    prev_chunknum = cur_chunknum
                    cur_chunk = [ ea, ea ]
                    chunks.append(cur_chunk)
                else:
                    cur_chunk[1] = ea

                s = util.scan_insn_for_debug_ref(ea)
                if s != None:
                    if not s in names:
                        names.append(s)

                if not iter.next_code():
                    break

            if len(names) == 1:
                for start_addr, end_addr in chunks:
                    f.write("0x%x;0x%x;%s\n" % \
                        (start_addr, end_addr, names[0]))

            i += 1

            progress = int((float(i) / float(len(funcs))) * 100.0)
            if progress - last_prog_update >= 10:
                print "  %d%%" % progress
                last_prog_update = progress

        f.close()
        print "Done"


if __name__ == "__main__":
    filename = AskFile(1, "*.osym", "Choose a filename for the result:")
    if filename != None:
        range = (0x0, 0xFFFFFFFF)
        extractor = DebugExtractor(range)
        extractor.extract(filename)

