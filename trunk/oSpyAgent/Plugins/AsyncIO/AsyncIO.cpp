#include <oSpyAgent/AgentPlugin.h>

class AsyncIOPlugin : public oSpy::AgentPlugin
{
public:
    virtual void Open ()
    {
        //MessageBoxW (NULL, L"Open", L"AsyncIOPlugin", MB_OK | MB_ICONINFORMATION);
    }

    virtual void Close ()
    {
        //MessageBoxW (NULL, L"Close", L"AsyncIOPlugin", MB_OK | MB_ICONINFORMATION);
    }
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"AsyncIO", L"Asynchronous IO tracker", AsyncIO);
