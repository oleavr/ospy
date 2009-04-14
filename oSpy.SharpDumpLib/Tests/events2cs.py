#!/usr/bin/env python

import sys

inputfile = sys.argv[1]
selectedindex = int(sys.argv[2])

f = open(inputfile)
buf = ''
i = 0
while True:
    startidx = buf.find('<event ')
    endidx = buf.find('</event>')
    if startidx < 0 or endidx < 0:
        chunk = f.read(16384)
        if chunk == '': break
        buf += chunk
        continue
    endidx += 8

    if i == selectedindex:
        eventxml = buf[startidx:endidx]
        lines = eventxml.split('\n')
        lines[0] = '\t' + lines[0]
        result = []
        for line in lines:
            convertedline = line
            if convertedline[0] == '\t':
                    convertedline = convertedline[1:]
            prefix = '                      '
            if result: prefix += '+'
            else: prefix += ' '
            indentspaces = ''
            for c in convertedline:
                if c == '\t':
                    indentspaces += '    '
                else:
                    break
            content = convertedline.replace('"', '\\"').replace('\t', '')
            convertedline = prefix + '%s"%s"' % (indentspaces, content)
            result.append(convertedline)

        result[-1] += ';'
        print "\n".join(result)
        break

    buf = buf[endidx:]
    i += 1
