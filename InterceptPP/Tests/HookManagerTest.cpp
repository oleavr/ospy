#include <InterceptPP/InterceptPP.h>
#include <InterceptPP/ConsoleLogger.h>
#include <iostream>

using namespace std;
using namespace InterceptPP;

#define DEBUG 0

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

