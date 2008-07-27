#include <oSpyAgent/AgentPlugin.h>

#pragma pack(push, 1)

typedef struct {
    HANDLE hDevice;
    DWORD dwIoControlCode;
    LPVOID lpInBuffer;
    DWORD nInBufferSize;
    LPVOID lpOutBuffer;
    DWORD nOutBufferSize;
    LPDWORD lpBytesReturned;
    LPOVERLAPPED lpOverlapped;
} AsyncIoControlArgs;

#pragma pack(pop, 1)

class AsyncIOPlugin : public oSpy::AgentPlugin
{
public:
    virtual bool Open ()
    {
        InterceptPP::FunctionSpec * spec = m_hookManager->GetFunctionSpecById ("DeviceIoControl");

        if (spec != NULL)
        {
            m_deviceIoControlHandler.Initialize (this, &AsyncIOPlugin::OnDeviceIoControl);
            spec->AddHandler (&m_deviceIoControlHandler);
        }

        return true;
    }

    virtual void Close ()
    {
    }

private:
    void OnDeviceIoControl (InterceptPP::FunctionCall * call, bool & shouldLog)
    {
        AsyncIoControlArgs * args = call->GetArgumentsPtr<AsyncIoControlArgs> ();

        //MessageBoxW (NULL, L"OnDeviceIoControl", L"AsyncIOPlugin", MB_OK | MB_ICONINFORMATION);
    }

    InterceptPP::FunctionCallHandler<AsyncIOPlugin> m_deviceIoControlHandler;
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"AsyncIO", L"Asynchronous IO tracker", AsyncIO);
