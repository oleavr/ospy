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

from idc import *
from idaapi import *
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

def scan_insn_for_debug_ref(ea):
    for j in xrange(10):
        op_type = GetOpType(ea, j)
        if op_type <= 0:
            break

        addr = None
        if op_type == 5: # immediate
            addr = GetOperandValue(ea, j)
        elif op_type == 2: # memory reference
            inslen = idaapi.ua_code(ea)
            assert inslen != 0
            insn = idaapi.get_current_instruction()
            assert insn
            op = idaapi.get_instruction_operand(insn, j)
            assert op

            addr = Dword(op.addr)
        else:
            addr = None

        if addr is not None and isData(GetFlags(addr)):
            s = _parse_string(addr)
            if s != None:
                s = s.split(" ", 1)[0]
                if s.find("::") != -1:
                    return _scrub_func_name(s)

    return None

def _parse_string(ea):
    reader = DataReader(ea)
    result = _do_parse_string(reader.read_u16_le)
    if result != None:  return result

    reader.reset()
    return _do_parse_string(reader.read_u8)

def _do_parse_string(reader_func):
    s = ""

    while True:
        value = reader_func()
        if value == 0:
            break
        elif not _is_plain_ascii(value):
            s = None
            break

        s += chr(value)

    return s

def _scrub_func_name(name):
    name = name.replace("%s", "")
    pos = name.find("::") + 2
    result = name[0:pos]
    for c in name[pos:]:
        if not _is_valid_identifier(ord(c)):
            break
        result += c
    return result

def _is_plain_ascii(b):
    return b in (9, 10, 13) or (b >= 32 and b <= 126)

def _is_valid_identifier(b):
    return (b >= 48 and b <= 57) or (b >= 65 and b <= 90) \
           or (b >= 97 and b <= 122) or b == 95 or b == 126

