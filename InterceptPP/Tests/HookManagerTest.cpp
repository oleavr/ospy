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
#include <iostream>

using namespace std;
using namespace InterceptPP;

#define DEBUG 1

int main(int argc, char *argv[])
{
    InterceptPP::Initialize();
    InterceptPP::SetLogger(new Logging::ConsoleLogger());

    HookManager *mgr = HookManager::Instance();
#if !DEBUG
    try
    {
#endif
        mgr->LoadDefinitions(L"D:\\Projects\\oSpy\\trunk\\oSpy\\bin\\Debug\\config.xml");

        cout << TypeBuilder::Instance()->GetTypeCount() << " types loaded" << endl;
        cout << mgr->GetFunctionSpecCount() << " FunctionSpec objects loaded" << endl;
        cout << mgr->GetVTableSpecCount() << " VTableSpec objects loaded" << endl;
        cout << mgr->GetSignatureCount() << " Signature objects loaded" << endl;
        cout << mgr->GetDllModuleCount() << " DllModule objects loaded" << endl;
        cout << mgr->GetDllFunctionCount() << " DllFunction objects loaded" << endl;
        cout << mgr->GetVTableCount() << " VTable objects loaded" << endl;
#if !DEBUG
    }
    catch (ParserError &e)
    {
        GetLogger()->LogError("LoadDefinitions failed: %s", e.what());
    }
    catch (...)
    {
        GetLogger()->LogError("LoadDefinitions failed: unknown error");
    }
#endif

    cout << "success" << endl;
    OString str;
    cin >> str;

	return 0;
}

