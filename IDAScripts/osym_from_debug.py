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

class DataReader:
    def __init__(self, ea):
        self._ea = ea
        self.reset()

    def reset(self):
        self.set_position(0)

    def set_position(self, pos):
        self._pos = pos

    def read_u8(self):
        ea = self._ea + self._pos
        if isCode(GetFlags(ea)):  return None
        self._pos += 1
        return Byte(ea)

    def read_u16_le(self):
        b1 = self.read_u8()
        b2 = self.read_u8()
        if None in (b1, b2):  return None
        return b1 | (b2 << 8)

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
            func_name = GetFunctionName(ea)
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

                name = GetMnem(ea)
                for j in xrange(10):
                    op_type = GetOpType(ea, j)
                    if op_type <= 0:
                        break
                    if op_type == 5: # immediate
                        op_value = GetOperandValue(ea, j)
                        if isData(GetFlags(op_value)):
                            result = self._parse_string(op_value)
                            if result != None:
                                s = result.split(" ", 2)[0]
                                if s.find("::") != -1:
                                    if not s in names:
                                        names.append(s)

                if not iter.next_code():
                    break

            if len(names) == 1:
                real_func_name = self._scrub_func_name(names[0])
                for start_addr, end_addr in chunks:
                    f.write( "0x%x;0x%x;%s\n" % \
                        (start_addr, end_addr, real_func_name))

            i += 1

            progress = int((float(i) / float(len(funcs))) * 100.0)
            if progress - last_prog_update >= 10:
                print "  %d%%" % progress
                last_prog_update = progress

        f.close()
        print "Done"

    def _parse_string(self, ea):
        reader = DataReader(ea)
        result = self._do_parse_string(reader.read_u16_le)
        if result != None:  return result

        reader.reset()
        return self._do_parse_string(reader.read_u8)

    def _do_parse_string(self, reader_func):
        s = ""

        while True:
            value = reader_func()
            if value == 0:
                break
            elif not self._is_plain_ascii(value):
                s = None
                break

            s += chr(value)

        return s

    def _scrub_func_name(self, name):
        name = name.replace("%s", "")
        pos = name.find("::") + 2
        result = name[0:pos]
        for c in name[pos:]:
            if not self._is_valid_identifier(ord(c)):
                break
            result += c
        return result

    def _is_plain_ascii(self, b):
        return b in (9, 10, 13) or (b >= 32 and b <= 126)

    def _is_valid_identifier(self, b):
        return (b >= 48 and b <= 57) or (b >= 65 and b <= 90) \
               or (b >= 97 and b <= 122) or b == 95 or b == 126


if __name__ == "__main__":
    filename = AskFile(1, "*.osym", "Choose a filename for the result:")
    if filename != None:
        range = (0x0, 0xFFFFFFFF)
        extractor = DebugExtractor(range)
        extractor.extract(filename)

