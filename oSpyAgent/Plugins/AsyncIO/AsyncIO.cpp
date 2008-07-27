#include <oSpyAgent/AgentPlugin.h>

#include <functional>
#include <algorithm>

class AsyncIOPlugin : public oSpy::AgentPlugin
{
public:
    AsyncIOPlugin ()
    {
        m_deviceIoControlHandler.Initialize (this, &AsyncIOPlugin::OnDeviceIoControl);
    }

    virtual void Open (InterceptPP::HookManager * mgr)
    {
        InterceptPP::FunctionSpec * spec = mgr->GetFunctionSpecById ("DeviceIoControl");

        if (spec != NULL)
        {
            spec->SetHandler (&m_deviceIoControlHandler);
        }
    }

    virtual void Close (InterceptPP::HookManager * mgr)
    {
    }

private:
    void OnDeviceIoControl (InterceptPP::FunctionCall * call, bool & shouldLog)
    {
        //MessageBoxW (NULL, L"OnDeviceIoControl", L"AsyncIOPlugin", MB_OK | MB_ICONINFORMATION);
    }

    InterceptPP::FunctionCallHandler<AsyncIOPlugin> m_deviceIoControlHandler;
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"AsyncIO", L"Asynchronous IO tracker", AsyncIO);
