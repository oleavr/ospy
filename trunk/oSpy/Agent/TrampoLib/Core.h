//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#pragma once

#include "Signature.h"

namespace TrampoLib {

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

typedef bool (*FunctionCallHandler) (FunctionCall *call);

class FunctionSpec : public BaseObject
{
public:
	FunctionSpec()
		: m_callingConvention(CALLING_CONV_UNKNOWN),
		  m_argsSize(FUNCTION_ARGS_SIZE_UNKNOWN),
		  m_enterHandler(NULL), m_leaveHandler(NULL)
	{}

	const OString &GetName() const { return m_name; }
	void SetName(const OString &name) { m_name = name; }

	CallingConvention GetCallingConvention() const { return m_callingConvention; }
	void SetCallingConvention(CallingConvention conv) { m_callingConvention = conv; }

	int GetArgsSize() const { return m_argsSize; }
	void SetArgsSize(int size) { m_argsSize = size; }

	void SetBasicParams(const OString &name, const int argsSize=FUNCTION_ARGS_SIZE_UNKNOWN)
	{
		SetName(name);
		SetArgsSize(argsSize);
	}

	FunctionCallHandler GetEnterHandler() const { return m_enterHandler; }
	void SetEnterHandler(FunctionCallHandler handler) { m_enterHandler = handler; }

	FunctionCallHandler GetLeaveHandler() const { return m_leaveHandler; }
	void SetLeaveHandler(FunctionCallHandler handler) { m_leaveHandler = handler; }

protected:
	OString m_name;
	CallingConvention m_callingConvention;
	int m_argsSize;
	FunctionCallHandler m_enterHandler;
	FunctionCallHandler m_leaveHandler;
};

class Function : public BaseObject
{
public:
    Function(FunctionSpec *spec=NULL, DWORD offset=0) { Initialize(spec, offset); }

    static void Initialize();
    void Initialize(FunctionSpec *spec, DWORD offset) { m_spec = spec; m_offset = offset; }

    virtual const OString GetParentName() const { return ""; }

    FunctionTrampoline *CreateTrampoline(unsigned int bytesToCopy=0);
    FunctionSpec *GetSpec() const { return m_spec; }
    DWORD GetOffset() const { return m_offset; }

    void Hook();

protected:
    FunctionSpec *m_spec;
    DWORD m_offset;
    static const PrologSignatureSpec prologSignatureSpecs[];
    static OVector<Signature>::Type prologSignatures;

    void OnEnter(FunctionCall *call);
    void OnLeave(FunctionCall *call);

private:
    static void OnEnterProxy(CpuContext cpuCtx, unsigned int unwindSize, FunctionTrampoline *trampoline, void **proxyRet, void **finalRet);
    FunctionTrampoline *OnEnterWrapper(CpuContext *cpuCtx, unsigned int *unwindSize, FunctionTrampoline *trampoline, void *btAddr, DWORD *lastError);

    static void OnLeaveProxy(CpuContext cpuCtx, FunctionTrampoline *trampoline);
    void OnLeaveWrapper(CpuContext *cpuCtx, FunctionTrampoline *trampoline, FunctionCall *call, DWORD *lastError);
};

class FunctionCall : public BaseObject
{
public:
	FunctionCall(Function *function, void *btAddr, CpuContext *cpuCtxEnter)
		: m_function(function), m_backtraceAddress(btAddr),
		  m_returnAddress(*((void **) btAddr)),
		  m_cpuCtxLive(NULL), m_cpuCtxEnter(*cpuCtxEnter),
		  m_lastErrorLive(NULL), m_shouldCarryOn(true)
	{
		memset(&m_cpuCtxLeave, 0, sizeof(m_cpuCtxLeave));

		int argsSize = function->GetSpec()->GetArgsSize();
		if (argsSize != FUNCTION_ARGS_SIZE_UNKNOWN)
		{
			m_argumentsData.resize(argsSize);
			memcpy((void *) m_argumentsData.data(), (BYTE *) btAddr + 4, argsSize);
		}
	}

	Function *GetFunction() const { return m_function; }
	const OString &GetArgumentsData() const { return m_argumentsData; }
	void *GetBacktraceAddress() const { return m_backtraceAddress; }
	void *GetReturnAddress() const { return m_returnAddress; }
	void SetCpuContextLive(CpuContext *cpuCtx) { m_cpuCtxLive = cpuCtx; }
	CpuContext *GetCpuContextLive() const { return m_cpuCtxLive; }
	const CpuContext *GetCpuContextEnter() const { return &m_cpuCtxEnter; }
	const CpuContext *GetCpuContextLeave() const { return &m_cpuCtxLeave; }
	void SetCpuContextLeave(const CpuContext *ctx) { m_cpuCtxLeave = *ctx; }
	DWORD *GetLastErrorLive() const { return m_lastErrorLive; }
	void SetLastErrorLive(DWORD *lastError) { m_lastErrorLive = lastError; }
	const OString &GetArgumentsData() { return m_argumentsData; }

	bool GetShouldCarryOn() const { return m_shouldCarryOn; }
	void SetShouldCarryOn(bool carryOn) { m_shouldCarryOn = carryOn; }

	OString ToString() const;

protected:
	Function *m_function;
	void *m_backtraceAddress;
	void *m_returnAddress;
	CpuContext *m_cpuCtxLive;
	CpuContext m_cpuCtxEnter;
	CpuContext m_cpuCtxLeave;
	DWORD *m_lastErrorLive;
	OString m_argumentsData;

	bool m_shouldCarryOn;
};

} // namespace TrampoLib
