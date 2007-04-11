//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#include <InterceptPP/InterceptPP.h>
#include <InterceptPP/ConsoleLogger.h>
#include <udis86.h>
#include <iostream>

using namespace std;
using namespace InterceptPP;

int main(int argc, char *argv[])
{
    Initialize();
    SetLogger(new Logging::ConsoleLogger());

    unsigned char buf[] = "\x6A\x20"
                          "\x68\xD8\xD2\xCB\x77"
                          "\xE8\x23\x8C\x01\x00";

    cout << "usage 1:" << endl;
    {
        ud_t udObj;
        ud_init(&udObj);
        ud_set_input_buffer(&udObj, buf, sizeof(buf) - 1);
	    ud_set_syntax(&udObj, UD_SYN_INTEL);

        while (true)
        {
            int size = ud_disassemble(&udObj);
            if (size == 0)
                break;

            cout << ud_insn_asm(&udObj) << " [size=" << size << "]" << endl;
        }
    }

    cout << endl << "usage 2:" << endl;
    {
        ud_t udObj;
        ud_init(&udObj);
        ud_set_input_buffer(&udObj, buf, sizeof(buf) - 1);
	    ud_set_mode(&udObj, 32);
	    ud_set_syntax(&udObj, UD_SYN_INTEL);

        while (true)
        {
            int size = ud_disassemble(&udObj);
            if (size == 0)
                break;

            cout << ud_insn_asm(&udObj) << " [size=" << size << "]" << endl;
        }
    }

    cout << endl << "success" << endl;
    OString str;
    cin >> str;

	return 0;
}

