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

#include <InterceptPP/Core.h>
#include <InterceptPP/ConsoleLogger.h>
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

    OString s1 =
        "6A 20"             // push 20h
        "68 D8 D2 CB 77"    // push offset dword_77CBD2D8
        "E8 23 8C 01 00";   // call __SEH_prolog4
    Signature sig1(s1);

    OString s2 =
        "6A ??"             // push ??
        "68 ?? ?? ?? ??"    // push ?? ?? ?? ??
        "E8 ?? ?? ?? ??";   // call ?? ?? ?? ??
    Signature sig2(s2);

    OVector<void *>::Type matches = SignatureMatcher::Instance()->FindInRange(sig1, buf, sizeof(buf));
    cout << "sig1, matches.size() == " << matches.size() << endl;

    matches = SignatureMatcher::Instance()->FindInRange(sig2, buf, sizeof(buf));
    cout << "sig2, matches.size() == " << matches.size() << endl;

    cout << "success" << endl;
    OString str;
    cin >> str;

	return 0;
}
