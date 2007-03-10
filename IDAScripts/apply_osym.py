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

if __name__ == "__main__":
    filename = AskFile(0, "*.osym", "Choose an oSym file to apply")
    if filename != None:
        f = open(filename, "rb")
        i = 0
        Batch(1)
        while True:
            line = f.readline()
            if len(line) == 0:
                break
            elif i > 0:
                begin, end, func_name = line.split(";", 2)
                begin = int(begin, 16)
                end = int(end, 16)
                func_name = func_name.rstrip()
                index = func_name.find("::~")
                if index != -1:
                    func_name = "%s::%s_dtor" % (func_name[0:index], func_name[index+3:])

                func = idaapi.get_func(begin)
                limits = idaapi.area_t()
                if idaapi.get_func_limits(func, limits):
                    if limits.startEA == begin:
                        print "Renaming 0x%x to %s" % (begin, func_name)
                        MakeName(begin, func_name)
            i += 1
        Batch(0)

