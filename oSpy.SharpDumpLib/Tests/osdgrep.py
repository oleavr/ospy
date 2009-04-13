#!/usr/bin/env python
# -*- coding: utf-8 -*-
#
# Copyright (C) 2009  Ole André Vadla Ravnås
#
# Search for events in an .osd, printing matching events to stdout,
# optionally stripping out backtrace and CPU registers information.
#
# Quick and superdirty implementation that's only intended for
# crafting datasets typically used in unit tests.
#

import sys
from xml.dom import minidom

if len(sys.argv) != 4:
    print >> sys.stderr, 'Invalid argument count.'
    print >> sys.stderr, 'Example: %s 0x8ac wlmdump.osd minimal' \
        % sys.argv[0]
    sys.exit(1)

searchstring = sys.argv[1].lower()
inputfile = sys.argv[2]
details = sys.argv[3]
assert details in ('minimal', 'full')

def remove_xmlelements_named(xml, name):
    while True:
        startidx = xml.find('<%s' % name)
        endidx = xml.find('</%s>' % name)
        if startidx < 0 or endidx < 0:
            break
        before = xml[:startidx]
        after = xml[endidx + len(name) + 3:]
        xml = before + after
    return xml

result = '<events>'

f = open(inputfile)
f.seek(8)
buf = ''
while True:
    startidx = buf.find('<event ')
    endidx = buf.find('</event>')
    if startidx < 0 or endidx < 0:
        chunk = f.read(16384)
        if chunk == '': break
        buf += chunk
        continue
    endidx += 8

    eventxml = buf[startidx:endidx]
    if searchstring in eventxml.lower():
        if details == 'minimal':
            eventxml = remove_xmlelements_named(eventxml, 'backtrace')
            eventxml = remove_xmlelements_named(eventxml, 'cpuContext')

        result += eventxml

    buf = buf[endidx:]

result += '</events>'

doc = minidom.parseString(result)
result = doc.toprettyxml()
print result

