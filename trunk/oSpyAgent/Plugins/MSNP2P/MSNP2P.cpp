#include <oSpyAgent/AgentPlugin.h>

typedef struct {
    DWORD field_0;
    void *m_object;
    DWORD field_8;
    DWORD field_C;
} CQueueElement;

typedef struct {
    CQueueElement **m_elements;
    DWORD field_4;
    DWORD m_numElements;
    float m_fVal1;
    float m_fVal2;
    float m_fVal3;
    DWORD field_18;
    DWORD field_1C;
    DWORD field_20;
    DWORD m_iVal;
    DWORD field_28;
    DWORD field_2C;
} CDynamicQueue;

const DWORD afterGetSendQueueNextHop = 0x4E4ECB;

static void
LogDynamicQueue (CDynamicQueue * queue)
{
    InterceptPP::Logging::Logger * logger = InterceptPP::GetLogger ();

    InterceptPP::Logging::Event * ev = logger->NewEvent ("Debug");

    InterceptPP::Logging::Element * queueNode = new InterceptPP::Logging::Element ("CDynamicQueue");

    InterceptPP::Logging::Element * elementsNode = new InterceptPP::Logging::Element ("m_elements");
    queueNode->AppendChild (elementsNode);

    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("field_4", queue->field_4));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("m_numElements", queue->m_numElements));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("m_fVal1", queue->m_fVal1));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("m_fVal2", queue->m_fVal2));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("m_fVal3", queue->m_fVal3));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("field_18", queue->field_18));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("field_1C", queue->field_1C));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("field_20", queue->field_20));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("m_iVal", queue->m_iVal));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("field_28", queue->field_28));
    queueNode->AppendChild (new InterceptPP::Logging::TextNode ("field_2C", queue->field_2C));

    if (queue->field_4 != 0)
    {
        for (unsigned int i = 0; i < queue->m_numElements; i++)
        {
            CQueueElement * element = queue->m_elements[i];

            InterceptPP::Logging::Element * elementNode = new InterceptPP::Logging::Element ("CQueueElement");
            if (element != NULL)
                elementNode->AddField ("Pointer", reinterpret_cast<DWORD> (element));
            else
                elementNode->AddField ("Pointer", "NULL");

            if (element != NULL)
            {
                elementNode->AppendChild (new InterceptPP::Logging::TextNode ("field_0", element->field_0));
                elementNode->AppendChild (new InterceptPP::Logging::TextNode ("m_object", element->m_object));
                elementNode->AppendChild (new InterceptPP::Logging::TextNode ("field_8", element->field_8));
                elementNode->AppendChild (new InterceptPP::Logging::TextNode ("field_C", element->field_C));
            }

            elementsNode->AppendChild (elementNode);
        }
    }

    ev->AppendChild (queueNode);

    logger->SubmitEvent (ev);
}

static __declspec(naked) void
PacketSchedulerRunAfterGetSendQueue ()
{
    CDynamicQueue * queue;

    __asm {
        pushad;

        push ebp;
        mov ebp, esp;
        sub esp, __LOCAL_SIZE;

        mov [queue], eax;
    }

    LogDynamicQueue (queue);

    __asm {
        leave;

        popad;

        lea ecx, [ebp-5Ch]; // Overwritten
        mov [ebp-1Ch], eax; //        code
        jmp [afterGetSendQueueNextHop];
    }
}

class MSNP2PPlugin : public oSpy::AgentPlugin
{
public:
    virtual bool Open ()
    {
        if (m_processName != "msnmsgr.exe")
            return false;

        m_wsoHandler.Initialize (this, &MSNP2PPlugin::OnWaitForSingleObject);
        m_wmoHandler.Initialize (this, &MSNP2PPlugin::OnWaitForMultipleObjects);

        InterceptPP::FunctionSpec * funcSpec;

        funcSpec = m_hookManager->GetFunctionSpecById ("WaitForSingleObject");
        if (funcSpec != NULL)
            funcSpec->AddHandler (&m_wsoHandler);

        funcSpec = m_hookManager->GetFunctionSpecById ("WaitForSingleObjectEx");
        if (funcSpec != NULL)
            funcSpec->AddHandler (&m_wsoHandler);

        funcSpec = m_hookManager->GetFunctionSpecById ("WaitForMultipleObjects");
        if (funcSpec != NULL)
            funcSpec->AddHandler (&m_wmoHandler);

        funcSpec = m_hookManager->GetFunctionSpecById ("WaitForMultipleObjectsEx");
        if (funcSpec != NULL)
            funcSpec->AddHandler (&m_wmoHandler);

        funcSpec = new InterceptPP::FunctionSpec ("CP2PTransport::SendControlPacket", InterceptPP::CALLING_CONV_STDCALL, 12);
        InterceptPP::Function *func = new InterceptPP::Function (funcSpec, 0x4EDD4A);
        func->Hook ();

        const DWORD getQueueOffset = 0x4E4EC5;
        DWORD oldMemProtect;
        if (!VirtualProtect (reinterpret_cast<LPVOID> (getQueueOffset), sizeof (InterceptPP::FunctionRedirectStub), PAGE_EXECUTE_WRITECOPY, &oldMemProtect))
            throw InterceptPP::Error("VirtualProtect failed");

        InterceptPP::FunctionRedirectStub *stub = reinterpret_cast<InterceptPP::FunctionRedirectStub *> (getQueueOffset);
        stub->JMP_opcode = 0xE9;
        stub->JMP_offset = reinterpret_cast<DWORD>(PacketSchedulerRunAfterGetSendQueue)
            - (reinterpret_cast<DWORD>(stub) + sizeof(InterceptPP::FunctionRedirectStub));

        return true;
    }

    virtual void Close ()
    {
    }

protected:
    void OnWaitForSingleObject (InterceptPP::FunctionCall *call, bool & shouldLog)
    {
        char * argumentList = call->GetArgumentsPtr<char> ();
        DWORD timeout = *reinterpret_cast<DWORD *> (argumentList + sizeof(HANDLE));

        shouldLog = (timeout != 0 && timeout != INFINITE);
    }

    void OnWaitForMultipleObjects (InterceptPP::FunctionCall *call, bool & shouldLog)
    {
        char * argumentList = call->GetArgumentsPtr<char> ();
        DWORD timeout = *reinterpret_cast<DWORD *>(argumentList + sizeof (DWORD) + sizeof (DWORD *) + sizeof (BOOL));

        shouldLog = (timeout != 0 && timeout != INFINITE);
    }

    InterceptPP::FunctionCallHandler<MSNP2PPlugin> m_wsoHandler;
    InterceptPP::FunctionCallHandler<MSNP2PPlugin> m_wmoHandler;
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"MSNP2P", L"MSNP2P research plugin", MSNP2P);
