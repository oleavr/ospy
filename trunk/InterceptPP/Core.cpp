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

#include "Core.h"
#include "NullLogger.h"
#include "HookManager.h"
#include "Util.h"
#include <udis86.h>

#define ENABLE_BACKTRACE_SUPPORT 1

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

using namespace Logging;

static CRITICAL_SECTION g_lock;
static Logger * g_logger = NULL;
static bool g_ownLogger = false;

#define INTERCEPT_PP_LOCK() EnterCriticalSection (&g_lock)
#define INTERCEPT_PP_UNLOCK() LeaveCriticalSection (&g_lock)

void
Initialize()
{
    InitializeCriticalSection (&g_lock);

    Function::Initialize ();
    Util::Instance()->Initialize ();
    SetLogger (NULL);
}

void
UnInitialize()
{
    HookManager::Instance()->Reset ();

    if (g_ownLogger)
        delete g_logger;
    g_logger = NULL;

    Util::Instance()->UnInitialize ();
    Function::UnInitialize ();

    DeleteCriticalSection (&g_lock);
}

Logger *
GetLogger ()
{
    Logger * logger;

    INTERCEPT_PP_LOCK ();
    logger = g_logger;
    INTERCEPT_PP_UNLOCK ();

    return logger;
}

void
SetLogger (Logger *logger)
{
    INTERCEPT_PP_LOCK ();

    if (g_ownLogger)
        delete g_logger;

    if (logger == NULL)
    {
        logger = new Logging::NullLogger ();
        g_ownLogger = true;
    }
    else
    {
        g_ownLogger = false;
    }

    g_logger = logger;

    INTERCEPT_PP_UNLOCK ();
}

#define TIB_MAGIC_OFFSET 700h

ReentranceProtector::ReentranceProtector ()
{
    m_oldValue = Protect ();
}

ReentranceProtector::~ReentranceProtector ()
{
    Unprotect (m_oldValue);
}

DWORD
ReentranceProtector::Protect ()
{
    DWORD oldValue;

    __asm
    {
        mov eax, fs:[TIB_MAGIC_OFFSET];
        mov [oldValue], eax;

        mov eax, [ReentranceProtector::MAGIC];
        mov fs:[TIB_MAGIC_OFFSET], eax;
    }

    return oldValue;
}

void
ReentranceProtector::Unprotect (DWORD oldValue)
{
    __asm
    {
        push eax;

        mov eax, [oldValue];
        mov fs:[TIB_MAGIC_OFFSET], eax;

        pop eax;
    }
}

const DWORD ReentranceProtector::MAGIC = 0x6F537079; // 'oSpy'

FARPROC Function::tlsGetValueFunc = NULL;
DWORD Function::tlsIdx = 0xFFFFFFFF;

const PrologSignatureSpec Function::prologSignatureSpecs[] = {
    {
        {
            NULL,
            0,
            "8B FF"                 // mov edi, edi
            "55"                    // push ebp
            "8B EC",                // mov ebp, esp
        },

        5,
    },
    {
        {
            NULL,
            0,
            "6A xx"                 // push xxh
            "68 xx xx xx xx"        // push offset dword_xxxxxxxx
            "E8 xx xx xx xx",       // call __SEH_prolog
        },

        7,
    },
    {
        {
            NULL,
            0,
            "68 xx xx xx xx"        // push xxxxxxxxh
            "68 xx xx xx xx"        // push offset dword_xxxxxxxx
            "E8 xx xx xx xx",       // call __SEH_prolog
        },

        5,
    },
    {
        {
            NULL,
            0,
            "FF 25 xx xx xx xx"     // jmp ds:__imp__*
        },

        6,
    },
    {
        {
            NULL,
            0,
            "33 C0"                 // xor eax, eax
            "50"                    // push eax
            "50"                    // push eax
            "6A xx"                 // push xx
        },

        6,
    },
};

OVector<Signature>::Type Function::prologSignatures;

volatile LONG Function::m_callsInProgress = 0;

Logging::Node *
Argument::ToNode(bool deep, IPropertyProvider *propProv) const
{
    Logging::Element *el = new Logging::Element("argument");
    el->AddField("name", m_spec->GetName());

    Logging::Node *valueNode = m_spec->GetMarshaller()->ToNode(m_data, deep, propProv);
    if (valueNode != NULL)
        el->AppendChild(valueNode);

    return el;
}

OString
Argument::ToString(bool deep, IPropertyProvider *propProv) const
{
    return m_spec->GetMarshaller()->ToString(m_data, deep, propProv);
}

bool
Argument::ToInt(int &result) const
{
    return m_spec->GetMarshaller()->ToInt(m_data, result);
}

bool
Argument::ToUInt(unsigned int &result) const
{
    return m_spec->GetMarshaller()->ToUInt(m_data, result);
}

bool
Argument::ToPointer(void *&result) const
{
    return m_spec->GetMarshaller()->ToPointer(m_data, result);
}

bool
Argument::ToVaList(va_list &result) const
{
    return m_spec->GetMarshaller()->ToVaList(m_data, result);
}

ArgumentListSpec::ArgumentListSpec()
{
    Initialize(0, NULL);
}

ArgumentListSpec::ArgumentListSpec(unsigned int count, ...)
{
    va_list args;
    va_start(args, count);

    Initialize(count, args);

    va_end(args);
}

ArgumentListSpec::ArgumentListSpec(unsigned int count, va_list args)
{
    Initialize(count, args);
}

ArgumentListSpec::~ArgumentListSpec()
{
    for (unsigned int i = 0; i < m_arguments.size(); i++)
    {
        delete m_arguments[i];
    }
}

void
ArgumentListSpec::Initialize(unsigned int count, va_list args)
{
    m_size = 0;
    m_hasOutArgs = false;

    for (unsigned int i = 0; i < count; i++)
    {
        ArgumentSpec *arg = va_arg(args, ArgumentSpec *);

        AddArgument(arg);
    }
}

void
ArgumentListSpec::AddArgument(ArgumentSpec *arg)
{
    m_arguments.push_back(arg);

    if ((arg->GetDirection() & ARG_DIR_OUT) != 0)
        m_hasOutArgs = true;

    arg->SetOffset(m_size);

    m_size += arg->GetMarshaller()->GetSize();
}

ArgumentList::ArgumentList(ArgumentListSpec *spec, void *data)
    : m_spec(spec)
{
    void *p = data;

    for (unsigned int i = 0; i < spec->GetCount(); i++)
    {
        ArgumentSpec *argSpec = (*spec)[i];

        m_arguments.push_back(Argument(argSpec, p));

        p = static_cast<unsigned char *>(p) + argSpec->GetSize();
    }
}

FunctionSpec::FunctionSpec(const OString &name,
                           CallingConvention conv,
                           int argsSize,
                           FunctionCallHandlerVector handlers,
                           bool logNestedCalls)
    : m_name (name),
      m_callingConvention (conv),
      m_argsSize (argsSize),
      m_argList (NULL),
      m_retValMarshaller (NULL),
      m_handlers (handlers),
      m_logNestedCalls (logNestedCalls)
{
}

FunctionSpec::~FunctionSpec()
{
    if (m_retValMarshaller)
        delete m_retValMarshaller;

    if (m_argList)
        delete m_argList;
}

void
FunctionSpec::SetParams (const OString & name,
                         CallingConvention conv,
                         int argsSize,
                         const FunctionCallHandlerVector & handlers,
                         bool logNestedCalls)
{
    SetName (name);
    SetCallingConvention (conv);
    SetArgsSize (argsSize);
    m_handlers = handlers;
    SetLogNestedCalls (logNestedCalls);
}

void
FunctionSpec::SetArguments(ArgumentListSpec *argList)
{
    if (argList != NULL)
    {
        m_argsSize = argList->GetSize();
        m_argList = argList;
    }
    else
    {
        m_argsSize = FUNCTION_ARGS_SIZE_UNKNOWN;
        m_argList = NULL;
    }
}

void
FunctionSpec::SetArguments(unsigned int count, ...)
{
    va_list args;
    va_start(args, count);

    m_argList = new ArgumentListSpec(count, args);
    m_argsSize = m_argList->GetSize();

    va_end(args);
}

const BaseMarshaller *
FunctionSpec::GetReturnValueMarshaller() const
{
    return m_retValMarshaller;
}

void
FunctionSpec::SetReturnValueMarshaller(BaseMarshaller *marshaller)
{
    if (m_retValMarshaller != NULL)
        delete m_retValMarshaller;
    m_retValMarshaller = marshaller;
}

Function::Function (FunctionSpec * spec, DWORD offset)
    : m_trampoline (NULL), m_oldMemProtect (0)
{
    Initialize (spec, offset);
}

Function::~Function ()
{
    delete m_trampoline;
    m_trampoline = NULL;
}

void
Function::Initialize ()
{
    tlsGetValueFunc = GetProcAddress (LoadLibraryW (L"kernel32.dll"), "TlsGetValue");
    tlsIdx = TlsAlloc ();

    for (int i = 0; i < sizeof (prologSignatureSpecs) / sizeof (PrologSignatureSpec); i++)
    {
        prologSignatures.push_back (Signature (prologSignatureSpecs[i].sig.signature));
    }
}

void
Function::UnInitialize ()
{
    prologSignatures.clear ();

    TlsFree (tlsIdx);
}

OString
Function::GetFullName () const
{
    OOStringStream ss;

    const OString &parentName = GetParentName ();
    if (parentName.length () > 0)
    {
        ss << parentName << "::";
    }

    ss << GetSpec ()->GetName ();

    return ss.str ();
}

#define OPCODE_CALL_RELATIVE 0xE8
#define OPCODE_JMP_RELATIVE  0xE9

FunctionTrampoline *
Function::CreateTrampoline (unsigned int bytesToCopy)
{
    int trampoSize = sizeof(FunctionTrampoline) + bytesToCopy + sizeof(FunctionRedirectStub);
    FunctionTrampoline *trampoline = reinterpret_cast<FunctionTrampoline *>(new unsigned char[trampoSize]);

    trampoline->CALL_opcode = OPCODE_CALL_RELATIVE;
    trampoline->CALL_offset = (DWORD) OnEnterProxy - (DWORD) &(trampoline->data);
    trampoline->data = this;

    if (bytesToCopy > 0)
    {
        memcpy(reinterpret_cast<unsigned char *>(trampoline) + sizeof(FunctionTrampoline), reinterpret_cast<const void *>(m_offset), bytesToCopy);
    }

    FunctionRedirectStub *redirStub = reinterpret_cast<FunctionRedirectStub *>(reinterpret_cast<unsigned char *>(trampoline) + sizeof(FunctionTrampoline) + bytesToCopy);
    redirStub->JMP_opcode = 0xE9;
    redirStub->JMP_offset = (m_offset + bytesToCopy) - (reinterpret_cast<DWORD>(reinterpret_cast<unsigned char *>(redirStub) + sizeof(FunctionRedirectStub)));

    DWORD oldProtect;
    if (!VirtualProtect(trampoline, trampoSize, PAGE_EXECUTE_READWRITE, &oldProtect))
        throw Error("VirtualProtected failed");

    return trampoline;
}

void
Function::Hook ()
{
    const PrologSignatureSpec * spec = NULL;
    int prologIndex = -1;
    int nBytesToCopy = 0;

    for (unsigned int i = 0; i < prologSignatures.size (); i++)
    {
        const Signature *sig = &prologSignatures[i];

        OVector<void *>::Type matches = SignatureMatcher::Instance ()->FindInRange (*sig, reinterpret_cast<void *> (m_offset), sig->GetLength ());
        if (matches.size () == 1)
        {
            spec = &prologSignatureSpecs[i];
            prologIndex = i;
            break;
        }
    }

    if (spec != NULL)
    {
        nBytesToCopy = spec->numBytesToCopy;
    }
    else
    {
        unsigned char * p = reinterpret_cast<unsigned char *> (m_offset);
        const int bytesNeeded = 5;

        ud_t udObj;
        ud_init (&udObj);
        ud_set_input_buffer (&udObj, p, 16);
        ud_set_mode (&udObj, 32);

        while (nBytesToCopy < bytesNeeded)
        {
            int size = ud_disassemble (&udObj);
            if (size == 0)
                throw Error ("none of the supported signatures matched and libudis86 fallback failed as well");

            nBytesToCopy += size;
        }
    }

#ifdef _INSANE_DEBUG
    Logging::Logger *logger = GetLogger ();
    if (logger != NULL)
    {
        if (prologIndex >= 0)
            logger->LogDebug ("%s: based on prologIndex %d we need to copy %d bytes of the original function", GetFullName ().c_str (), prologIndex, nBytesToCopy);
        else
            logger->LogDebug ("%s: based on runtime disassembly we need to copy %d bytes of the original function", GetFullName ().c_str (), nBytesToCopy);
    }
#endif

    FunctionTrampoline * trampoline = CreateTrampoline (nBytesToCopy);
    m_trampoline = trampoline;

    if (!VirtualProtect (reinterpret_cast<LPVOID> (m_offset), sizeof (LONGLONG), PAGE_EXECUTE_READWRITE, &m_oldMemProtect))
        throw Error ("VirtualProtected failed");

    // Make two copies of the start of the function:
    //  1) m_origStart: need it to revert in Unhook()
    //  2) buf: our working copy that we'll modify and copy back
    FunctionRedirectStub * redirStub = reinterpret_cast<FunctionRedirectStub *> (m_offset);

    memcpy (m_origStart, redirStub, sizeof(m_origStart));

    unsigned char buf[8];
    memcpy (buf, redirStub, sizeof (buf));

    // Modify 2)
    // JMP to the trampoline
    FunctionRedirectStub *stub = reinterpret_cast<FunctionRedirectStub *> (buf);
    stub->JMP_opcode = OPCODE_JMP_RELATIVE;
    stub->JMP_offset = reinterpret_cast<DWORD> (trampoline) - (reinterpret_cast<DWORD> (reinterpret_cast<unsigned char *> (redirStub) + sizeof (FunctionRedirectStub)));

    // Copy back 2)
    // HACK: make sure we start with a fresh timeslice
    //       (we might be able to fix this later on by doing the swap atomically)
    Sleep (0);
    memcpy (redirStub, buf, sizeof(buf));
    FlushInstructionCache (GetCurrentProcess (), NULL, 0);
}

void
Function::Unhook()
{
    DWORD oldProtect;
    if (!VirtualProtect (reinterpret_cast<LPVOID> (m_offset), sizeof (LONGLONG), PAGE_EXECUTE_READWRITE, &oldProtect))
        throw Error ("VirtualProtected failed");

    // HACK: see above
    Sleep (0);
    memcpy (reinterpret_cast<void *> (m_offset), m_origStart, sizeof (m_origStart));

    VirtualProtect (reinterpret_cast<LPVOID> (m_offset), sizeof (LONGLONG), m_oldMemProtect, &oldProtect);
    FlushInstructionCache (GetCurrentProcess (), NULL, 0);
}

void
Function::WaitForCallsToComplete ()
{
    Sleep (15);

    while (m_callsInProgress > 0)
    {
        Sleep (30);
    }

    Sleep (15);
}

__declspec(naked) void
Function::OnEnterProxy(CpuContext cpuCtx, DWORD cpuFlags, unsigned int unwindSize, FunctionTrampoline *trampoline, void **proxyRet, void **finalRet)
{
    DWORD oldProtect, lastError;
    Function *function;
    FunctionTrampoline *nextTrampoline;

    __asm {
                                            // *** We're coming in hot from the modified prolog/vtable through the trampoline ***

        pushfd;
        pushad;

        mov eax, fs:[TIB_MAGIC_OFFSET];     // Protect against re-entrance (if we call a hooked function from the logging code)
        cmp eax, [ReentranceProtector::MAGIC];
        jz SHORT_CIRCUIT;

        push [tlsIdx];                      // Nested call?
        call [tlsGetValueFunc];
        test eax, eax;
        jnz SHORT_CIRCUIT;

        popad;
        popfd;
        jmp CARRY_ON;

SHORT_CIRCUIT:
        popad;                              // Just short-circuit this trampoline; no interception desired.
        popfd;
        add [esp], 4;
        ret;

CARRY_ON:
        push eax;                           //  1. Reserve space for the last 3 arguments.
        push eax;                           //     We avoid using sub here because we don't want to modify any flags.
        push eax;

        push 16;                            //  2. Set unwindSize to the size of the last 4 arguments.

        pushfd;                             //  3. Save all flags and registers and place them so that they're available
        pushad;                             //     from C++ through the first two arguments.
                                            //

        lea eax, [esp+52+4];                //  4. Set finalRet to point to the final return address.
        mov [esp+52-4], eax;

        lea eax, [esp+52+0];                //  5. Set proxyRet to point to this function's return address.
        mov [esp+52-8], eax;

        mov eax, [eax];                     //  6. Set trampoline to point to the start of the trampoline, ie. *proxyRet - 5.
        sub eax, 5;
        mov [esp+52-12], eax;

        sub esp, 4;                         //  7. Padding/fake return address so that ebp+8 refers to the first argument.
        push ebp;                           //  8. Standard prolog.
        mov ebp, esp;
        sub esp, __LOCAL_SIZE;
    }

    InterlockedIncrement (&m_callsInProgress);

    oldProtect = ReentranceProtector::Protect ();
    lastError = GetLastError();

    function = static_cast<Function *>(trampoline->data);

    if (!function->GetSpec()->GetLogNestedCalls())
        TlsSetValue(tlsIdx, reinterpret_cast<LPVOID>(1));

    nextTrampoline = function->OnEnterWrapper(&cpuCtx, &unwindSize, trampoline, finalRet, &lastError);
    if (nextTrampoline != NULL)
    {
        *proxyRet = reinterpret_cast<unsigned char *>(trampoline) + sizeof(FunctionTrampoline);
        *finalRet = nextTrampoline;
    }

    SetLastError(lastError);
    ReentranceProtector::Unprotect (oldProtect);

    if (nextTrampoline == NULL)
        InterlockedDecrement (&m_callsInProgress);

    __asm {
                                            // *** Bounce off to the actual method, or straight back to the caller. ***

        mov esp, ebp;                       //  1. Standard epilog.
        pop ebp;
        add esp, 4;                         //  2. Remove the padding/fake return address (see step 7 above).

        popad;                              //  3. Clean up the first argument and restore registers and flags (see step 3 above).
        popfd;

        add esp, [esp];                     //  4. Clean up the remaining arguments (and more if returning straight back).

        ret;
    }
}

FunctionTrampoline *
Function::OnEnterWrapper(CpuContext *cpuCtx, unsigned int *unwindSize, FunctionTrampoline *trampoline, void *btAddr, DWORD *lastError)
{
    // Keep track of the function call
    FunctionCall *call = new FunctionCall(this, btAddr, cpuCtx);
    call->SetCpuContextLive(cpuCtx);
    call->SetLastErrorLive(lastError);

    OnEnter(call);

    bool carryOn = call->GetShouldCarryOn();

    FunctionSpec *spec = call->GetFunction()->GetSpec();
    CallingConvention conv = spec->GetCallingConvention();
    if (!carryOn && (conv == CALLING_CONV_UNKNOWN ||
            (conv != CALLING_CONV_CDECL && spec->GetArgsSize() == FUNCTION_ARGS_SIZE_UNKNOWN)))
    {
        Logger *logger = GetLogger();
        if (logger != NULL)
            logger->LogWarning("Ignoring ShouldCarryOn override for %s because of lack of information",
                spec->GetName().c_str());
        carryOn = true;
    }

    if (carryOn)
    {
        // Set up a trampoline used to trap the return
        FunctionTrampoline *retTrampoline = new FunctionTrampoline;

        retTrampoline->CALL_opcode = 0xE8;
        retTrampoline->CALL_offset = (DWORD) Function::OnLeaveProxy - (DWORD) &(retTrampoline->data);
        retTrampoline->data = call;

        DWORD oldProtect;
        VirtualProtect(retTrampoline, sizeof(FunctionTrampoline), PAGE_EXECUTE_READWRITE, &oldProtect);

        return retTrampoline;
    }
    else
    {
        TlsSetValue(tlsIdx, NULL);

        // Clear off the proxy return address.
        *unwindSize += sizeof(void *);

        if (conv != CALLING_CONV_CDECL)
        {
            *unwindSize += spec->GetArgsSize();

            void **retAddr = reinterpret_cast<void **>(static_cast<char *>(btAddr) + spec->GetArgsSize());
            *retAddr = call->GetReturnAddress();
        }
    }

    delete call;
    return NULL;
}

__declspec(naked) void
Function::OnLeaveProxy(CpuContext cpuCtx, DWORD cpuFlags, FunctionTrampoline *trampoline)
{
    FunctionCall *call;
    DWORD oldProtect, lastError;

    __asm {
                                            // *** We're coming in hot and the method has just been called ***

        push eax;                           //  1. Reserve space for the third argument to this function (FunctionTrampoline *).
                                            //     We avoid using sub here because we don't want to modify any flags.
        pushfd;                             //  2. Save flags and conveniently place them so that they're available from C++ through
                                            //     the second argument.

        push eax;
        push ebx;

        mov eax, [esp+8+8];                 //  3. Get the trampoline returnaddress, which is the address of the VMethodCall *
                                            //     right after the CALL instruction on the trampoline.
        mov ebx, eax;                       //  4. Store the VMethodCall ** in ebx.
        mov ebx, [ebx];                     //  5. Dereference the VMethodCall **.
        sub eax, 5;                         //  6. Rewind the pointer to the start of the VMethodTrampoline structure.
        mov [esp+8+4], eax;                 //  7. Store the FunctionTrampoline * on the reserved spot so that we can access it from
                                            //     C++ through the second argument.
        mov eax, [ebx+FunctionCall::m_returnAddress];    //  8. Get the return address of the caller.
        mov [esp+8+8], eax;                 //  9. Replace the trampoline return-address with the return address of the caller.

        pop ebx;
        pop eax;

        pushad;                             // 10. Save all registers and conveniently place them as the first argument.

        sub esp, 4;                         // 11. Padding/fake return address so that ebp+8 refers to the first argument.
        push ebp;                           // 12. Standard prolog.
        mov ebp, esp;
        sub esp, __LOCAL_SIZE;
    }

    oldProtect = ReentranceProtector::Protect ();
    lastError = GetLastError();

    call = static_cast<FunctionCall *>(trampoline->data);
    call->GetFunction()->OnLeaveWrapper(&cpuCtx, trampoline, call, &lastError);

    TlsSetValue(tlsIdx, NULL);

    SetLastError(lastError);
    ReentranceProtector::Unprotect (oldProtect);

    InterlockedDecrement (&m_callsInProgress);

    __asm {
                                            // *** Bounce off back to the caller ***

        mov esp, ebp;                       //  1. Standard epilog.
        pop ebp;
        add esp, 4;                         //  2. Remove the padding/fake return address (see step 11 above).

        popad;                              //  3. Clean up the first two arguments and restore the registers and flags (see steps 2 and 10 above).
        popfd;

        add esp, 4;                         //  4. Clean up the second argument.
        ret;                                //  5. Bounce to the caller.
    }
}

void
Function::OnLeaveWrapper(CpuContext *cpuCtx, FunctionTrampoline *trampoline, FunctionCall *call, DWORD *lastError)
{
    call->SetState(FUNCTION_CALL_LEAVING);

    call->SetCpuContextLive(cpuCtx);
    call->SetLastErrorLive(lastError);

    // Got this now
    call->SetCpuContextLeave(cpuCtx);

    // Do some logging
    OnLeave(call);

    delete trampoline;
    delete call;
}

void
Function::OnEnter(FunctionCall *call)
{
    bool shouldLog = true;
    const FunctionCallHandlerVector & handlers = call->GetFunction ()->GetSpec ()->GetHandlers ();
    if (handlers.size () > 0)
    {
        FunctionCallHandlerVector::const_iterator it;

        for (it = handlers.begin (); it != handlers.end (); it++)
            (**it) (call, shouldLog);
    }

    if (shouldLog)
    {
        Logging::Event *ev = GetLogger()->NewEvent("FunctionCall");

        Logging::TextNode *textNode = new Logging::TextNode("name", GetFullName());
        ev->AppendChild(textNode);

        call->AppendBacktraceToElement(ev);
        call->AppendCpuContextToElement(ev);
        call->AppendArgumentsToElement(ev);

        if (call->GetShouldCarryOn())
            call->SetUserData(ev);
        else
            ev->Submit();
    }
}

void
Function::OnLeave(FunctionCall *call)
{
    bool shouldLog = true;
    const FunctionCallHandlerVector & handlers = call->GetFunction ()->GetSpec ()->GetHandlers ();
    if (handlers.size () > 0)
    {
        FunctionCallHandlerVector::const_iterator it;

        for (it = handlers.begin (); it != handlers.end (); it++)
            (**it) (call, shouldLog);
    }

    if (shouldLog)
    {
        Logging::Event *ev = static_cast<Logging::Event *>(call->GetUserData());
        if (ev != NULL)
        {
            call->AppendCpuContextToElement(ev);
            call->AppendArgumentsToElement(ev);
            call->AppendReturnValueToElement(ev);

            ev->Submit();
        }
    }
}

FunctionCall::FunctionCall(Function *function, void *btAddr, CpuContext *cpuCtxEnter)
    : m_function(function), m_backtraceAddress(btAddr),
      m_returnAddress(*((void **) btAddr)),
      m_cpuCtxLive(NULL), m_cpuCtxEnter(*cpuCtxEnter),
      m_lastErrorLive(NULL),
      m_arguments(NULL),
      m_state(FUNCTION_CALL_ENTERING),
      m_shouldCarryOn(true),
      m_userData(NULL)
{
    memset(&m_cpuCtxLeave, 0, sizeof(m_cpuCtxLeave));

    int argsSize = function->GetSpec()->GetArgsSize();
    if (argsSize != FUNCTION_ARGS_SIZE_UNKNOWN)
    {
        if (argsSize > 0)
        {
            m_argumentsData.resize(argsSize);
            memcpy((void *) m_argumentsData.data(), (BYTE *) btAddr + 4, argsSize);
        }

        ArgumentListSpec *spec = function->GetSpec()->GetArguments();
        if (spec != NULL)
        {
            m_arguments = new ArgumentList(spec, const_cast<void *>(static_cast<const void *>(m_argumentsData.data())));
        }
    }
}

bool
FunctionCall::ShouldLogArgumentDeep(const Argument *arg) const
{
    ArgumentSpec *spec = arg->GetSpec();
    ArgumentDirection dir = spec->GetDirection();

    if (m_state == FUNCTION_CALL_ENTERING)
    {
        return (dir & ARG_DIR_IN) != 0;
    }
    else
    {
        bool shouldLog = (dir & ARG_DIR_OUT) != 0;
        if (!shouldLog)
            return false;

        // TODO: we only check this when leaving for now -- does it make any sense to do it on entry?
        return spec->ShouldLogEval(&m_cpuCtxLeave);
    }
}

void
FunctionCall::AppendBacktraceToElement(Logging::Element *el)
{
#if ENABLE_BACKTRACE_SUPPORT
    Logging::Node *btNode = Util::Instance()->CreateBacktraceNode(m_backtraceAddress);
    if (btNode != NULL)
    {
        el->AppendChild(btNode);
    }
#endif
}

void
FunctionCall::AppendCpuContextToElement(Logging::Element *el)
{
    Logging::Element *ctxEl = new Logging::Element("cpuContext");

    ctxEl->AddField("direction", (m_state == FUNCTION_CALL_ENTERING) ? "in" : "out");

    AppendCpuRegisterToElement(ctxEl, "eax", m_cpuCtxLive->eax);
    AppendCpuRegisterToElement(ctxEl, "ebx", m_cpuCtxLive->ebx);
    AppendCpuRegisterToElement(ctxEl, "ecx", m_cpuCtxLive->ecx);
    AppendCpuRegisterToElement(ctxEl, "edx", m_cpuCtxLive->edx);
    AppendCpuRegisterToElement(ctxEl, "edi", m_cpuCtxLive->edi);
    AppendCpuRegisterToElement(ctxEl, "esi", m_cpuCtxLive->esi);
    AppendCpuRegisterToElement(ctxEl, "ebp", m_cpuCtxLive->ebp);
    AppendCpuRegisterToElement(ctxEl, "esp", m_cpuCtxLive->esp);

    el->AppendChild(ctxEl);
}

void
FunctionCall::AppendCpuRegisterToElement(Logging::Element *el, const char *name, DWORD value)
{
    Logging::Element *regEl = new Logging::Element("register");
    el->AppendChild(regEl);
    regEl->AddField("name", name);

    OOStringStream ss;
    if (value != 0)
        ss << "0x" << hex;
    ss << value;
    regEl->AddField("value", ss.str());
}

void
FunctionCall::AppendArgumentsToElement(Logging::Element *el)
{
    FunctionSpec *spec = m_function->GetSpec();

    const ArgumentList *args = GetArguments();
    if (args != NULL)
    {
        bool logIt = true;

        // Do we have any out arguments?
        if (m_state == FUNCTION_CALL_LEAVING && !args->GetSpec()->GetHasOutArgs())
        {
            logIt = false;
        }

        if (logIt)
        {
            Logging::Element *argsEl = new Logging::Element("arguments");
            el->AppendChild(argsEl);
            argsEl->AddField("direction", (m_state == FUNCTION_CALL_ENTERING) ? "in" : "out");

            for (unsigned int i = 0; i < args->GetCount(); i++)
            {
                const Argument &arg = (*args)[i];

                if (!(m_state == FUNCTION_CALL_LEAVING && arg.GetSpec()->GetDirection() == ARG_DIR_IN))
                    argsEl->AppendChild(arg.ToNode(ShouldLogArgumentDeep(&arg), this));
            }
        }
    }
    else
    {
        // No point in logging for this state in this case
        if (m_state == FUNCTION_CALL_LEAVING)
            return;

        Logging::Element *argsEl = new Logging::Element("arguments");
        el->AppendChild(argsEl);
        argsEl->AddField("direction", "in");

        int argsSize = spec->GetArgsSize();
        if (argsSize != FUNCTION_ARGS_SIZE_UNKNOWN && argsSize % sizeof(DWORD) == 0)
        {
            DWORD *args = (DWORD *) m_argumentsData.data();

            Marshaller::UInt32 marshaller;

            for (unsigned int i = 0; i < argsSize / sizeof(DWORD); i++)
            {
                Logging::Element *argElement = new Logging::Element("argument");
                argsEl->AppendChild(argElement);

                OOStringStream ss;
                ss << "arg" << (i + 1);
                argElement->AddField("name", ss.str());

                bool hex = false;

                // FIXME: optimize this
                if (args[i] > 0xFFFF && !IsBadReadPtr((void *) args[i], 1))
                    hex = true;

                marshaller.SetFormatHex(hex);

                Logging::Node *valueNode = marshaller.ToNode(&args[i], true, this);
                if (valueNode != NULL)
                    argElement->AppendChild(valueNode);
            }
        }
    }
}

void
FunctionCall::AppendReturnValueToElement(Logging::Element *el)
{
    if (m_state != FUNCTION_CALL_LEAVING)
        return;

    const BaseMarshaller *marshaller = m_function->GetSpec()->GetReturnValueMarshaller();
    if (marshaller == NULL)
        return;

    Logging::Element *retEl = new Logging::Element("returnValue");
    el->AppendChild(retEl);

    void *start = &(m_cpuCtxLive->eax);
    retEl->AppendChild(marshaller->ToNode(start, true, this));
}

OString
FunctionCall::ToString()
{
    FunctionSpec *spec = m_function->GetSpec();

    OOStringStream ss;

    ss << m_function->GetFullName();

    const ArgumentList *args = GetArguments();
    if (args != NULL)
    {
        ss << "(";

        for (unsigned int i = 0; i < args->GetCount(); i++)
        {
            const Argument &arg = (*args)[i];

            if (i)
                ss << ", ";

            ss << arg.ToString(ShouldLogArgumentDeep(&arg), this);
        }

        ss << ")";
    }
    else
    {
        int argsSize = spec->GetArgsSize();
        if (argsSize != FUNCTION_ARGS_SIZE_UNKNOWN && argsSize % sizeof(DWORD) == 0)
        {
            ss << "(";

            DWORD *args = (DWORD *) m_argumentsData.data();

            for (unsigned int i = 0; i < argsSize / sizeof(DWORD); i++)
            {
                if (i)
                    ss << ", ";

                // FIXME: optimize this
                if (args[i] > 0xFFFF && !IsBadReadPtr((void *) args[i], 1))
                    ss << hex << "0x";
                else
                    ss << dec;

                ss << args[i];
            }

            ss << ")";
        }
    }

    return ss.str();
}

bool
FunctionCall::QueryForProperty(const OString &query, int &result)
{
    const Argument *arg;
    DWORD reg;
    bool isArg, wantAddrOf;

    if (!ResolveProperty(query, arg, reg, isArg, wantAddrOf))
        return false;

    if (wantAddrOf)
        return false;

    if (isArg)
        return arg->ToInt(result);

    result = reg;
    return true;
}

bool
FunctionCall::QueryForProperty(const OString &query, unsigned int &result)
{
    const Argument *arg;
    DWORD reg;
    bool isArg, wantAddrOf;

    if (!ResolveProperty(query, arg, reg, isArg, wantAddrOf))
        return false;

    if (wantAddrOf)
        return false;

    if (isArg)
        return arg->ToUInt(result);

    result = reg;
    return true;
}

bool
FunctionCall::QueryForProperty(const OString &query, void *&result)
{
    const Argument *arg;
    DWORD reg;
    bool isArg, wantAddrOf;

    if (!ResolveProperty(query, arg, reg, isArg, wantAddrOf))
        return false;

    if (isArg)
    {
        if (!wantAddrOf)
        {
            return arg->ToPointer(result);
        }
        else
        {
            if (m_state != FUNCTION_CALL_ENTERING)
                return false;

            result = static_cast<unsigned char *>(m_backtraceAddress) + 4 + arg->GetSpec()->GetOffset();
            return true;
        }
    }
    else
    {
        if (wantAddrOf)
            return false;

        result = reinterpret_cast<void *>(reg);
        return true;
    }
}

bool
FunctionCall::QueryForProperty(const OString &query, va_list &result)
{
    const Argument *arg;
    DWORD reg;
    bool isArg, wantAddrOf;

    if (!ResolveProperty(query, arg, reg, isArg, wantAddrOf))
        return false;

    if (wantAddrOf)
        return false;

    if (isArg)
        return arg->ToVaList(result);
    else
        return false;
}

bool
FunctionCall::QueryForProperty(const OString &query, OString &result)
{
    const Argument *arg;
    DWORD reg;
    bool isArg, wantAddrOf;

    if (!ResolveProperty(query, arg, reg, isArg, wantAddrOf))
        return false;

    if (wantAddrOf)
        return false;

    if (!isArg)
        return false;

    result = arg->ToString(true, this);
    return true;
}

bool
FunctionCall::ResolveProperty(const OString &query, const Argument *&arg, DWORD &reg, bool &isArgument, bool &wantAddressOf)
{
    // minimum: "arg.s"
    if (query.size() < 5)
        return false;

    int off = 0;
    if (query[0] == '&')
    {
        wantAddressOf = true;
        off++;
    }
    else
    {
        wantAddressOf = false;
    }

    OString propObj = query.substr(off, off + 4);
    OString propArg = query.substr(off + 4);

    if (propObj == "reg.")
    {
        if (propArg == "eax")
            reg = m_cpuCtxLive->eax;
        else if (propArg == "ebx")
            reg = m_cpuCtxLive->ebx;
        else if (propArg == "ecx")
            reg = m_cpuCtxLive->ecx;
        else if (propArg == "edx")
            reg = m_cpuCtxLive->edx;
        else if (propArg == "edi")
            reg = m_cpuCtxLive->edi;
        else if (propArg == "esi")
            reg = m_cpuCtxLive->esi;
        else if (propArg == "ebp")
            reg = m_cpuCtxLive->ebp;
        else if (propArg == "esp")
            reg = m_cpuCtxLive->esp;
        else
            return false;

        isArgument = false;
        return true;
    }
    else if (propObj == "arg.")
    {
        for (unsigned int i = 0; i < m_arguments->GetCount(); i++)
        {
            const Argument &curArg = (*m_arguments)[i];

            if (curArg.GetSpec()->GetName() == propArg)
            {
                arg = &curArg;
                isArgument = true;
                return true;
            }
        }
    }

    return false;
}

} // namespace InterceptPP
