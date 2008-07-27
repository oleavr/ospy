#include <oSpyAgent/AgentPlugin.h>

class TemplatePlugin : public oSpy::AgentPlugin
{
public:
    virtual void Open (InterceptPP::HookManager * mgr)
    {
    }

    virtual void Close (InterceptPP::HookManager * mgr)
    {
    }
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"Template", L"Template plugin", Template);
