#include <oSpyAgent/AgentPlugin.h>

class TemplatePlugin : public oSpy::AgentPlugin
{
public:
    virtual void Open ()
    {
    }

    virtual void Close ()
    {
    }
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"Template", L"Template plugin", Template);
