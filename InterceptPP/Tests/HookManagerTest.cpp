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
