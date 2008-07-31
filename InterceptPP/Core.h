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

#pragma once

#include "InterceptPP.h"
#include "Errors.h"
#include "Marshallers.h"
#include "Signature.h"
#include "Logging.h"

namespace InterceptPP {

#pragma warning (push)
#pragma warning (disable: 4251)

INTERCEPTPP_API void Initialize();
INTERCEPTPP_API void UnInitialize();
INTERCEPTPP_API Logging::Logger *GetLogger();
INTERCEPTPP_API void SetLogger(Logging::Logger *logger);

typedef struct {
    DWORD edi;
    DWORD esi;
    DWORD ebp;
    DWORD esp;
    DWORD ebx;
    DWORD edx;
    DWORD ecx;
    DWORD eax;
} CpuContext;

typedef enum {
    CALLING_CONV_UNKNOWN = 0,
    CALLING_CONV_STDCALL,
    CALLING_CONV_THISCALL,
    CALLING_CONV_CDECL,
} CallingConvention;

typedef enum {
    FUNCTION_CALL_ENTERING,
    FUNCTION_CALL_LEAVING,
} FunctionCallState;

typedef enum {
    ARG_DIR_UNKNOWN = 0,
    ARG_DIR_IN      = 1,
    ARG_DIR_OUT     = 2,
} ArgumentDirection;

#pragma pack(push, 1)
typedef struct {
    BYTE CALL_opcode;
    DWORD CALL_offset;
    void *data;
} FunctionTrampoline;

typedef struct {
    BYTE JMP_opcode;
    DWORD JMP_offset;
} FunctionRedirectStub;
#pragma pack(pop)

typedef struct {
    SignatureSpec sig;
    int numBytesToCopy;
} PrologSignatureSpec;

#define FUNCTION_ARGS_SIZE_UNKNOWN -1

class FunctionCall;

class IFunctionCallHandler
{
public:
    virtual void operator() (FunctionCall * call, bool & shouldLog) = 0;
};

template<class Class>
class FunctionCallHandler : public IFunctionCallHandler
{
public:
    typedef void (Class::*Method) (FunctionCall * call, bool & shouldLog);

    FunctionCallHandler ()
        : m_instance (NULL),
          m_method (NULL)
    {
    }

    FunctionCallHandler (Class * instance, Method method)
        : m_instance (instance),
          m_method (method)
    {
    }

    void Initialize (Class * instance, Method method)
    {
        m_instance = instance;
        m_method = method;
    }

    virtual void operator() (FunctionCall * call, bool & shouldLog)
    {
        (m_instance->*m_method) (call, shouldLog);
    }

private:
    Class * m_instance;
    Method m_method;
};

typedef bool (__stdcall *RegisterEvalFunc) (const CpuContext *context);

class INTERCEPTPP_API ReentranceProtector
{
public:
    ReentranceProtector ();
    ~ReentranceProtector ();

    static DWORD Protect ();
    static void Unprotect (DWORD oldValue);

    static const DWORD MAGIC;

private:
    DWORD m_oldValue;
};

class INTERCEPTPP_API ArgumentSpec : public BaseObject
{
public:
    ArgumentSpec (const OString & name, ArgumentDirection direction, BaseMarshaller * marshaller, RegisterEvalFunc shouldLogRegEval)
        : m_name (name), m_direction (direction), m_offset (0), m_marshallerIn (marshaller), m_marshallerOut (marshaller), m_shouldLogRegEval (shouldLogRegEval)
    {
    }

    ArgumentSpec (const OString & name, ArgumentDirection direction, BaseMarshaller * marshallerIn, BaseMarshaller * marshallerOut, RegisterEvalFunc shouldLogRegEval)
        : m_name (name), m_direction (direction), m_offset (0), m_marshallerIn (marshallerIn), m_marshallerOut (marshallerOut), m_shouldLogRegEval (shouldLogRegEval)
    {
    }

    ~ArgumentSpec()
    {
        if (m_marshallerIn == m_marshallerOut)
        {
            delete m_marshallerIn;
        }
        else
        {
            delete m_marshallerIn;
            delete m_marshallerOut;
        }

        if (m_shouldLogRegEval != NULL)
            delete[] ((BYTE *)  m_shouldLogRegEval);
    }

    const OString &GetName() const { return m_name; }
    ArgumentDirection GetDirection() const { return m_direction; }
    const BaseMarshaller * GetMarshaller (ArgumentDirection direction) const
    {
        if (direction == ARG_DIR_UNKNOWN)
            return (m_marshallerIn != NULL) ? m_marshallerIn : m_marshallerOut;
        else
            return (direction == ARG_DIR_IN) ? m_marshallerIn : m_marshallerOut;
    }

    unsigned int GetOffset() const { return m_offset; }
    void SetOffset(unsigned int offset) { m_offset = offset; }

    unsigned int GetSize() const
    {
        return (m_marshallerIn != NULL) ? m_marshallerIn->GetSize () : m_marshallerOut->GetSize ();
    }

    bool ShouldLogEval(const CpuContext *ctx) const
    {
        if (m_shouldLogRegEval == NULL)
            return true;
        return m_shouldLogRegEval(ctx);
    }

protected:
    OString m_name;
    ArgumentDirection m_direction;
    unsigned int m_offset;
    BaseMarshaller * m_marshallerIn;
    BaseMarshaller * m_marshallerOut;

    RegisterEvalFunc m_shouldLogRegEval;
};

class INTERCEPTPP_API Argument : public BaseObject
{
public:
    Argument(ArgumentSpec * spec, void * data)
        : m_spec (spec), m_data (data)
    {}

    ArgumentSpec * GetSpec () const { return m_spec; }

    Logging::Node * ToNode (ArgumentDirection direction, bool deep, IPropertyProvider * propProv) const;
    OString ToString (ArgumentDirection direction, bool deep, IPropertyProvider * propProv) const;
    bool ToInt (ArgumentDirection direction, int & result) const;
    bool ToUInt (ArgumentDirection direction, unsigned int & result) const;
    bool ToPointer (ArgumentDirection direction, void *& result) const;
    bool ToVaList (ArgumentDirection direction, va_list & result) const;

protected:
    ArgumentSpec * m_spec;
    void * m_data;
};

class INTERCEPTPP_API ArgumentListSpec : public BaseObject
{
public:
    ArgumentListSpec();
    ArgumentListSpec(unsigned int count, ...);
    ArgumentListSpec(unsigned int count, va_list args);
    ~ArgumentListSpec();

    void AddArgument(ArgumentSpec *arg);

    unsigned int GetSize() const { return m_size; }
    unsigned int GetCount() const { return static_cast<unsigned int>(m_arguments.size()); }
    bool GetHasOutArgs() const { return m_hasOutArgs; }

    ArgumentSpec *operator[](int index) { return m_arguments[index]; }

protected:
    unsigned int m_size;
    OVector<ArgumentSpec *>::Type m_arguments;
    bool m_hasOutArgs;

    void Initialize(unsigned int count, va_list args);
};

class INTERCEPTPP_API ArgumentList : public BaseObject
{
public:
    ArgumentList(ArgumentListSpec *spec, void *data);
    ~ArgumentList();

    const ArgumentListSpec *GetSpec() const { return m_spec; }

    unsigned int GetCount() const { return static_cast<unsigned int>(m_arguments.size()); }

    const Argument &operator[](int index) const { return m_arguments[index]; }

protected:
    ArgumentListSpec *m_spec;
    OVector<Argument>::Type m_arguments;
};

typedef OVector<IFunctionCallHandler *>::Type FunctionCallHandlerVector;

class INTERCEPTPP_API FunctionSpec : public BaseObject
{
public:
    FunctionSpec(const OString &name="",
                 CallingConvention conv=CALLING_CONV_UNKNOWN,
                 int argsSize=FUNCTION_ARGS_SIZE_UNKNOWN,
                 FunctionCallHandlerVector handlers=FunctionCallHandlerVector(),
                 bool logNestedCalls=false);
    ~FunctionSpec();

    void SetParams(const OString &name,
                   CallingConvention conv=CALLING_CONV_UNKNOWN,
                   int argsSize=FUNCTION_ARGS_SIZE_UNKNOWN,
                   const FunctionCallHandlerVector &handlers=FunctionCallHandlerVector(),
                   bool logNestedCalls=false);

    ArgumentListSpec *GetArguments() const { return m_argList; }
    void SetArguments(ArgumentListSpec *argList);
    void SetArguments(unsigned int count, ...);

    const BaseMarshaller *GetReturnValueMarshaller() const;
    void SetReturnValueMarshaller(BaseMarshaller *marshaller);

    const OString &GetName() const { return m_name; }
    void SetName(const OString &name) { m_name = name; }

    CallingConvention GetCallingConvention() const { return m_callingConvention; }
    void SetCallingConvention(CallingConvention conv) { m_callingConvention = conv; }

    int GetArgsSize() const { return m_argsSize; }
    void SetArgsSize(int size) { m_argsSize = size; }

    const FunctionCallHandlerVector & GetHandlers () const { return m_handlers; }
    void AddHandler (IFunctionCallHandler * handler) { m_handlers.push_back (handler); }

    bool GetLogNestedCalls() const { return m_logNestedCalls; }
    void SetLogNestedCalls(bool logNestedCalls) { m_logNestedCalls = logNestedCalls; }

protected:
    OString m_name;
    CallingConvention m_callingConvention;
    int m_argsSize;
    ArgumentListSpec *m_argList;
    BaseMarshaller *m_retValMarshaller;
    FunctionCallHandlerVector m_handlers;
    bool m_logNestedCalls;
};

class INTERCEPTPP_API Function : public BaseObject
{
public:
    Function (FunctionSpec *spec = NULL, DWORD offset = 0);
    ~Function ();

    static void Initialize ();
    static void UnInitialize ();
    void Initialize (FunctionSpec * spec, DWORD offset) { m_spec = spec; m_offset = offset; }

    virtual const OString GetParentName () const { return ""; }
    OString GetFullName () const;

    FunctionTrampoline * CreateTrampoline (unsigned int bytesToCopy = 0);
    FunctionSpec * GetSpec () const { return m_spec; }
    DWORD GetOffset () const { return m_offset; }

    void Hook ();
    void Unhook ();

    static void WaitForCallsToComplete ();

protected:
    FunctionSpec * m_spec;
    DWORD m_offset;

    static FARPROC tlsGetValueFunc;
    static DWORD tlsIdx;

    static const PrologSignatureSpec prologSignatureSpecs[];
    static OVector<Signature>::Type prologSignatures;

    void * m_trampoline;
    DWORD m_oldMemProtect;
    unsigned char m_origStart[8];

    static volatile LONG m_callsInProgress;

    void OnEnter (FunctionCall * call);
    void OnLeave (FunctionCall * call);

private:
    static void OnEnterProxy (CpuContext cpuCtx, DWORD cpuFlags, unsigned int unwindSize, FunctionTrampoline * trampoline, void ** proxyRet, void ** finalRet);
    FunctionTrampoline * OnEnterWrapper (CpuContext * cpuCtx, unsigned int * unwindSize, FunctionTrampoline * trampoline, void * btAddr, DWORD * lastError);

    static void OnLeaveProxy (CpuContext cpuCtx, DWORD cpuFlags, FunctionTrampoline * trampoline);
    void OnLeaveWrapper (CpuContext * cpuCtx, FunctionTrampoline * trampoline, FunctionCall * call, DWORD * lastError);
};

class INTERCEPTPP_API FunctionCall : public BaseObject, IPropertyProvider
{
public:
    FunctionCall (Function * function, void * btAddr, CpuContext * cpuCtxEnter);

    Function *GetFunction () const { return m_function; }
    void *GetBacktraceAddress () const { return m_backtraceAddress; }
    void *GetReturnAddress () const { return m_returnAddress; }

    CpuContext * GetCpuContextLive () const { return m_cpuCtxLive; }
    void SetCpuContextLive (CpuContext * cpuCtx) { m_cpuCtxLive = cpuCtx; }

    const CpuContext * GetCpuContextEnter () const { return &m_cpuCtxEnter; }

    const CpuContext * GetCpuContextLeave () const { return &m_cpuCtxLeave; }
    void SetCpuContextLeave (const CpuContext * ctx) { m_cpuCtxLeave = *ctx; }

    DWORD GetLastError () const { return *m_lastErrorLive; }
    DWORD *GetLastErrorLive () const { return m_lastErrorLive; }
    void SetLastErrorLive (DWORD *lastError) { m_lastErrorLive = lastError; }

    const ArgumentList * GetArguments () const { return m_arguments; }
    const OString & GetArgumentsData () const { return m_argumentsData; }
    template<typename T> T * GetArgumentsPtr () const { return reinterpret_cast<T *> (const_cast<char *> (m_argumentsData.data ())); }
    template<typename T> T * GetArgumentsPtrLive () const { return reinterpret_cast<T *> (static_cast<char *> (m_backtraceAddress) + sizeof (void *)); }

    DWORD GetReturnValue () { return m_cpuCtxLeave.eax; }

    FunctionCallState GetState () const { return m_state; }
    void SetState (FunctionCallState state) { m_state = state; }

    bool GetShouldCarryOn () const { return m_shouldCarryOn; }
    void SetShouldCarryOn (bool carryOn) { m_shouldCarryOn = carryOn; }

    Logging::Event * GetLogEvent () const { return m_logEvent; }
    void SetLogEvent (Logging::Event * ev) { m_logEvent = ev; }

    void *GetUserData () const { return m_userData; }
    template<typename T> T * GetUserData () const { return static_cast<T *> (m_userData); }
    void SetUserData (void *data) { m_userData = data; }

    void AppendBacktraceToElement (Logging::Element * el);
    void AppendCpuContextToElement (Logging::Element * el);
    void AppendArgumentsToElement (Logging::Element * el);
    void AppendReturnValueToElement (Logging::Element * el);
    void AppendLastErrorToElement (Logging::Element * el);
    OString ToString ();

    virtual bool QueryForProperty (const OString &query, int & result);
    virtual bool QueryForProperty (const OString &query, unsigned int & result);
    virtual bool QueryForProperty (const OString &query, void *& result);
    virtual bool QueryForProperty (const OString &query, va_list & result);
    virtual bool QueryForProperty (const OString &query, OString & result);

protected:
    Function * m_function;
    void * m_backtraceAddress;
    void * m_returnAddress;
    CpuContext * m_cpuCtxLive;
    CpuContext m_cpuCtxEnter;
    CpuContext m_cpuCtxLeave;
    DWORD * m_lastErrorLive;

    OString m_argumentsData;
    ArgumentList * m_arguments;

    FunctionCallState m_state;

    bool m_shouldCarryOn;

    Logging::Event * m_logEvent;
    void * m_userData;

private:
    bool ShouldLogArgumentDeep (const Argument * arg) const;
    inline ArgumentDirection GetCurrentArgumentDirection () const { return (m_state == FUNCTION_CALL_ENTERING) ? ARG_DIR_IN : ARG_DIR_OUT; }
    void AppendCpuRegisterToElement (Logging::Element * el, const char * name, DWORD value);

    bool ResolveProperty (const OString & query, const Argument *& arg, DWORD & reg, bool & isArgument, bool & wantAddressOf);
};

#pragma warning (pop)

} // namespace InterceptPP
