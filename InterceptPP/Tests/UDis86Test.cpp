//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
