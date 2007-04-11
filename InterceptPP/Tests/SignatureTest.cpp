#include <InterceptPP/InterceptPP.h>
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

    OString s =
        "6A 20"             // push 20h
        "68 D8 D2 CB 77"    // push offset dword_77CBD2D8
        "E8 23 8C 01 00";   // call __SEH_prolog4
    Signature sig(s);

    OVector<void *>::Type matches = SignatureMatcher::Instance()->FindInRange(sig, buf, sizeof(buf));
    cout << "matches.size() == " << matches.size() << endl;

    cout << "success" << endl;
    OString str;
    cin >> str;

	return 0;
}

