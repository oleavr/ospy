#include <oSpyAgent/AgentPlugin.h>

#include <ks.h>
#include <ksmedia.h>

using namespace InterceptPP;

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
    TrackedIoControl (DWORD code, void * outBuffer, DWORD outBufferSize, DWORD * bytesReturned, OVERLAPPED * overlapped, unsigned int logEventId)
        : m_code (code),
          m_outBuffer (outBuffer),
          m_outBufferSize (outBufferSize),
          m_bytesReturned (bytesReturned),
          m_overlapped (overlapped),
          m_logEventId (logEventId)
    {
    }

    DWORD m_code;
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
        FunctionSpec * dicSpec = m_hookManager->GetFunctionSpecById ("DeviceIoControl");
        FunctionSpec * gorSpec = m_hookManager->GetFunctionSpecById ("GetOverlappedResult");

        if (dicSpec == NULL || gorSpec == NULL)
            return false;

        m_dicHandler.Initialize (this, &AsyncIOPlugin::OnDeviceIoControl);
        m_dicOutBufferLogHandler.Initialize (this, &AsyncIOPlugin::LogDeviceIoControlOutBuffer);
        m_gorHandler.Initialize (this, &AsyncIOPlugin::OnGetOverlappedResult);

        ArgumentSpec * dicOutBufArg = (*dicSpec->GetArguments ())["lpOutBuffer"];
        dicSpec->AddHandler (&m_dicHandler);
        dicOutBufArg->AddLogHandler (&m_dicOutBufferLogHandler);
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

    void OnDeviceIoControl (FunctionCall * call, bool & shouldLog)
    {
        AsyncIoControlArgs * origArgs = call->GetArgumentsPtr<AsyncIoControlArgs> ();
        if (origArgs->lpOverlapped == NULL)
            return;

        if (call->GetState () == FUNCTION_CALL_LEAVING)
        {
            if (call->GetReturnValue () == FALSE && call->GetLastError () == ERROR_IO_PENDING)
            {
                OVERLAPPED * overlapped = origArgs->lpOverlapped;

                TrackedIoControl * ctx = new TrackedIoControl (
                    origArgs->dwIoControlCode, origArgs->lpOutBuffer, origArgs->nOutBufferSize,
                    origArgs->lpBytesReturned, overlapped, call->GetLogEvent ()->GetId ());

                this->Lock ();
                if (Event::IsValidHandle (overlapped->hEvent))
                    m_eventToIoControl[overlapped->hEvent] = ctx;
                m_overlappedToIoControl[overlapped] = ctx;
                this->Unlock ();
            }
        }
    }

    void AppendKsReadStreamToElement (const void * headerData, DWORD headerSize, bool full, Logging::Element * el)
    {
        if (headerSize < sizeof (KSSTREAM_HEADER))
            return;

        const KSSTREAM_HEADER * streamHdr = static_cast<const KSSTREAM_HEADER *> (headerData);

        Logging::DataNode * streamHdrNode = new Logging::DataNode ("value");
        streamHdrNode->AddField ("type", "KSSTREAM_HEADER");
        streamHdrNode->AddField ("size", sizeof (KSSTREAM_HEADER));
        streamHdrNode->SetData (streamHdr, sizeof (KSSTREAM_HEADER));
        el->AppendChild (streamHdrNode);

        if (headerSize >= sizeof (KSSTREAM_HEADER) + sizeof (KS_FRAME_INFO))
        {
            const KS_FRAME_INFO * frameInfo = reinterpret_cast<const KS_FRAME_INFO *> (streamHdr + 1);

            Logging::DataNode * frameInfoNode = new Logging::DataNode ("value");
            frameInfoNode->AddField ("type", "KS_FRAME_INFO");
            frameInfoNode->AddField ("size", sizeof (KS_FRAME_INFO));
            frameInfoNode->SetData (frameInfo, sizeof (KS_FRAME_INFO));
            el->AppendChild (frameInfoNode);
        }

        Logging::DataNode * streamBufNode = new Logging::DataNode ("value");
        streamBufNode->AddField ("type", "ByteArray");
        streamBufNode->AddField ("size", streamHdr->FrameExtent);
        if (full && streamHdr->Data != NULL && streamHdr->FrameExtent != 0)
            streamBufNode->SetData (streamHdr->Data, streamHdr->FrameExtent);
        el->AppendChild (streamBufNode);
    }

    bool LogDeviceIoControlOutBuffer (const FunctionCall * call, const ArgumentList * argList, const Argument * arg, Logging::Element * argElement)
    {
        bool entering = (call->GetState () == FUNCTION_CALL_ENTERING);
        if (!entering && call->GetReturnValue () == FALSE)
            return false;

        DWORD code = (*argList)["dwIoControlCode"].GetValue<DWORD> ();
        if (code != IOCTL_KS_READ_STREAM)
            return false;

        const void * hdrData = arg->GetValue<void *> ();
        DWORD hdrSize = (entering) ? (*argList)["nOutBufferSize"].GetValue<DWORD> () : *((*argList)["lpBytesReturned"].GetValue<DWORD *> ());
        bool full = !entering;
        AppendKsReadStreamToElement (hdrData, hdrSize, full, argElement);

        return true;
    }

    void OnGetOverlappedResult (FunctionCall * call, bool & shouldLog)
    {
        shouldLog = false;

        if (call->GetState () == FUNCTION_CALL_LEAVING && call->GetReturnValue () == TRUE)
        {
            GetOverlappedResultArgs * origArgs = call->GetArgumentsPtr<GetOverlappedResultArgs> ();

            this->Lock ();

            OverlappedToIoControlMap::iterator it = m_overlappedToIoControl.find (origArgs->lpOverlapped);
            if (it != m_overlappedToIoControl.end ())
            {
                TrackedIoControl * ctx = it->second;

                Logging::Event * ev = m_logger->NewEvent ("AsyncResult");

                Logging::TextNode * reqIdNode = new Logging::TextNode ("requestId");
                OOStringStream ss;
                ss << ctx->m_logEventId;
                reqIdNode->SetText (ss.str ());
                ev->AppendChild (reqIdNode);

                Logging::Element * dataEl = new Logging::Element ("data");
                ev->AppendChild (dataEl);

                const void * outData = static_cast<void *> (ctx->m_outBuffer);
                DWORD outDataSize = *origArgs->lpNumberOfBytesTransferred;

                if (ctx->m_code == IOCTL_KS_READ_STREAM)
                {
                    bool full = true;
                    AppendKsReadStreamToElement (outData, outDataSize, full, dataEl);
                }

                ev->Submit ();

                m_overlappedToIoControl.erase (it);
                delete ctx;
            }

            this->Unlock ();
        }
    }

    CRITICAL_SECTION m_lock;

    FunctionCallHandler<AsyncIOPlugin> m_dicHandler;
    ArgumentLogHandler<AsyncIOPlugin> m_dicOutBufferLogHandler;
    FunctionCallHandler<AsyncIOPlugin> m_gorHandler;

    typedef OMap<HANDLE, TrackedIoControl *>::Type EventToIoControlMap;
    typedef OMap<OVERLAPPED *, TrackedIoControl *>::Type OverlappedToIoControlMap;
    EventToIoControlMap m_eventToIoControl;
    OverlappedToIoControlMap m_overlappedToIoControl;
};

OSPY_AGENT_PLUGIN_DEFINE (1, L"AsyncIO", L"Asynchronous IO tracker", AsyncIO);
