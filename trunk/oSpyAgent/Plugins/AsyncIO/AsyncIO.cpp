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

typedef struct {
    HANDLE hFile;
    LPOVERLAPPED lpOverlapped;
    LPDWORD lpNumberOfBytesTransferred;
    BOOL bWait;
} GetOverlappedResultArgs;

#pragma pack(pop, 1)

class Event
{
public:
    static bool IsValidHandle (HANDLE ev) { return (ev != NULL && ev != INVALID_HANDLE_VALUE); }
};

class TrackedIoControl
{
public:
    TrackedIoControl (void * outBuffer, DWORD outBufferSize, DWORD * bytesReturned, OVERLAPPED * overlapped, unsigned int logEventId)
        : m_outBuffer (outBuffer),
          m_outBufferSize (outBufferSize),
          m_bytesReturned (bytesReturned),
          m_overlapped (overlapped),
          m_logEventId (logEventId)
    {
    }

    void * m_outBuffer;
    DWORD m_outBufferSize;
    DWORD * m_bytesReturned;
    OVERLAPPED * m_overlapped;
    unsigned int m_logEventId;
};

class AsyncIOPlugin : public oSpy::AgentPlugin
{
public:
    virtual bool Open ()
    {
        InterceptPP::FunctionSpec * dicSpec = m_hookManager->GetFunctionSpecById ("DeviceIoControl");
        InterceptPP::FunctionSpec * gorSpec = m_hookManager->GetFunctionSpecById ("GetOverlappedResult");

        if (dicSpec == NULL || gorSpec == NULL)
            return false;

        m_dicHandler.Initialize (this, &AsyncIOPlugin::OnDeviceIoControl);
        m_gorHandler.Initialize (this, &AsyncIOPlugin::OnGetOverlappedResult);

        dicSpec->AddHandler (&m_dicHandler);
        gorSpec->AddHandler (&m_gorHandler);

        InitializeCriticalSection (&m_lock);

        return true;
    }

    virtual void Close ()
    {
        DeleteCriticalSection (&m_lock);
    }

private:
    void Lock () { EnterCriticalSection (&m_lock); }
    void Unlock () { LeaveCriticalSection (&m_lock); }

    void OnDeviceIoControl (InterceptPP::FunctionCall * call, bool & shouldLog)
    {
        AsyncIoControlArgs * origArgs = call->GetArgumentsPtr<AsyncIoControlArgs> ();
        if (origArgs->lpOverlapped == NULL)
            return;

        if (call->GetState () == InterceptPP::FUNCTION_CALL_LEAVING)
        {
            if (call->GetReturnValue () == FALSE && call->GetLastError () == ERROR_IO_PENDING)
            {
                OVERLAPPED * overlapped = origArgs->lpOverlapped;

                TrackedIoControl * ctx = new TrackedIoControl (
                    origArgs->lpOutBuffer, origArgs->nOutBufferSize,
                    origArgs->lpBytesReturned, overlapped,
                    call->GetLogEvent ()->GetId ());

                this->Lock ();
                if (Event::IsValidHandle (overlapped->hEvent))
                    m_eventToIoControl[overlapped->hEvent] = ctx;
                m_overlappedToIoControl[overlapped] = ctx;
                this->Unlock ();
            }
        }
    }

    void OnGetOverlappedResult (InterceptPP::FunctionCall * call, bool & shouldLog)
    {
        shouldLog = false;

        if (call->GetState () == InterceptPP::FUNCTION_CALL_LEAVING && call->GetReturnValue () == TRUE)
        {
            GetOverlappedResultArgs * origArgs = call->GetArgumentsPtr<GetOverlappedResultArgs> ();

            this->Lock ();

            OverlappedToIoControlMap::iterator it = m_overlappedToIoControl.find (origArgs->lpOverlapped);
            if (it != m_overlappedToIoControl.end ())
            {
                TrackedIoControl * ctx = it->second;

                InterceptPP::Logging::Event * ev = m_logger->NewEvent ("AsyncResult");
                InterceptPP::Logging::DataNode * dataNode = new InterceptPP::Logging::DataNode ("data");
                dataNode->AddField ("id", ctx->m_logEventId);
                dataNode->AddField ("size", *origArgs->lpNumberOfBytesTransferred);
                dataNode->SetData (ctx->m_outBuffer, *origArgs->lpNumberOfBytesTransferred);
                ev->AppendChild (dataNode);
                ev->Submit ();

                m_overlappedToIoControl.erase (it);
                delete ctx;
            }

            this->Unlock ();
        }
    }

    CRITICAL_SECTION m_lock;

    InterceptPP::FunctionCallHandler<AsyncIOPlugin> m_dicHandler;
    InterceptPP::FunctionCallHandler<AsyncIOPlugin> m_gorHandler;

    typedef InterceptPP::OMap<HANDLE, TrackedIoControl *>::Type EventToIoControlMap;
    typedef InterceptPP::OMap<OVERLAPPED *, TrackedIoControl *>::Type OverlappedToIoControlMap;
    EventToIoControlMap m_eventToIoControl;
    OverlappedToIoControlMap m_overlappedToIoControl;
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"AsyncIO", L"Asynchronous IO tracker", AsyncIO);
