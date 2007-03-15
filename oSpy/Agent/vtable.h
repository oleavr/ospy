//
// Copyright (C) 2007  Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

#pragma once

#include "hooking.h"

#define VMETHOD_ARGS_SIZE_UNKNOWN -1

class VTableSpec;
class VMethodSpec;
class VTable;
class VMethod;
class VMethodCall;

#pragma pack(push, 1)
typedef struct {
	BYTE CALL_opcode;
	DWORD CALL_offset;
	void *data;
} VMethodTrampoline;
#pragma pack(pop)

typedef enum {
	CALLING_CONV_UNKNOWN = 0,
	CALLING_CONV_STDCALL,
	CALLING_CONV_THISCALL,
	CALLING_CONV_CDECL,
} CallingConvention;

class VTableSpec : public BaseObject
{
public:
	VTableSpec(const OString &name, int methodCount);

	const OString &GetName() const { return m_name; }
	int GetMethodCount() const { return (int) m_methods.size(); }
	VMethodSpec &GetMethodByIndex(int index) { return m_methods[index]; }

	VMethodSpec &operator[](int index) { return m_methods[index]; }

protected:
	OString m_name;
	OVector<VMethodSpec>::Type m_methods;
};

typedef bool (*VMethodCallHandler) (VMethodCall *call);

class VMethodSpec : public BaseObject
{
public:
	VMethodSpec()
		: m_vtable(NULL), m_index(-1),
		  m_callingConvention(CALLING_CONV_UNKNOWN),
		  m_argsSize(VMETHOD_ARGS_SIZE_UNKNOWN),
		  m_enterHandler(NULL), m_leaveHandler(NULL)
	{}
	void Initialize(VTableSpec *vtable, int index);

	const VTableSpec *GetVTable() { return m_vtable; }
	int GetIndex() const { return m_index; }

	const OString &GetName() const { return m_name; }
	void SetName(const OString &name) { m_name = name; }

	CallingConvention GetCallingConvention() const { return m_callingConvention; }
	void SetCallingConvention(CallingConvention conv) { m_callingConvention = conv; }

	int GetArgsSize() const { return m_argsSize; }
	void SetArgsSize(int size) { m_argsSize = size; }

	void SetBasicParams(const OString &name, const int argsSize=VMETHOD_ARGS_SIZE_UNKNOWN)
	{
		SetName(name);
		SetArgsSize(argsSize);
	}

	VMethodCallHandler GetEnterHandler() const { return m_enterHandler; }
	void SetEnterHandler(VMethodCallHandler handler) { m_enterHandler = handler; }

	VMethodCallHandler GetLeaveHandler() const { return m_leaveHandler; }
	void SetLeaveHandler(VMethodCallHandler handler) { m_leaveHandler = handler; }

protected:
	VTableSpec *m_vtable;
	int m_index;
	OString m_name;
	CallingConvention m_callingConvention;
	int m_argsSize;
	VMethodCallHandler m_enterHandler;
	VMethodCallHandler m_leaveHandler;
};

class VTable : public BaseObject
{
public:
	VTable(VTableSpec *spec, const OString &name, DWORD startOffset);

	const OString &GetName() const { return m_name; }
	VTableSpec *GetSpec() { return m_spec; }
	DWORD GetStartOffset() const { return m_startOffset; }
	VMethod &GetMethodByIndex(int index) { return m_methods[index]; }

	void Hook();

	VMethod &operator[](int index) { return m_methods[index]; }

protected:
	VTableSpec *m_spec;
	OString m_name;
	DWORD m_startOffset;
	OVector<VMethod>::Type m_methods;
};

class VMethod : public BaseObject
{
public:
	void Initialize(VMethodSpec *spec, VTable *vtable, DWORD offset) { m_spec = spec; m_vtable = vtable; m_offset = offset; }

	VMethodTrampoline *CreateTrampoline();

	VMethodSpec *GetSpec() const { return m_spec; }
	VTable *GetVTable() const { return m_vtable; }
	DWORD GetOffset() const { return m_offset; }

protected:
	VMethodSpec *m_spec;
	VTable *m_vtable;
	DWORD m_offset;

	void OnEnter(VMethodCall *call);
	void OnLeave(VMethodCall *call);

private:
	static void OnEnterProxy(CpuContext cpuCtx, VMethodTrampoline *trampoline);
	VMethodTrampoline *OnEnterWrapper(CpuContext *cpuCtx, VMethodTrampoline *trampoline, void *btAddr, DWORD *lastError);

	static void OnLeaveProxy(CpuContext cpuCtx, VMethodTrampoline *trampoline);
	void OnLeaveWrapper(CpuContext *cpuCtx, VMethodTrampoline *trampoline, VMethodCall *call);
};

class VMethodCall : public BaseObject
{
public:
	VMethodCall(VMethod *method, void *btAddr, CpuContext *cpuCtxEnter)
		: m_method(method), m_backtraceAddress(btAddr),
		  m_returnAddress(*((void **) btAddr)),
		  m_cpuCtxLive(NULL), m_cpuCtxEnter(*cpuCtxEnter),
		  m_lastErrorLive(NULL), m_shouldCarryOn(true)
	{
		memset(&m_cpuCtxLeave, 0, sizeof(m_cpuCtxLeave));

		int argsSize = method->GetSpec()->GetArgsSize();
		if (argsSize != VMETHOD_ARGS_SIZE_UNKNOWN)
		{
			m_argumentsData.resize(argsSize);
			memcpy((void *) m_argumentsData.data(), (BYTE *) btAddr + 4, argsSize);
		}
	}

	VMethod *GetMethod() const { return m_method; }
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
	VMethod *m_method;
	void *m_backtraceAddress;
	void *m_returnAddress;
	CpuContext *m_cpuCtxLive;
	CpuContext m_cpuCtxEnter;
	CpuContext m_cpuCtxLeave;
	DWORD *m_lastErrorLive;
	OString m_argumentsData;

	bool m_shouldCarryOn;
};
