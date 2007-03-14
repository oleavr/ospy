#pragma once

#include "hooking.h"

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

class VTableSpec : public BaseObject
{
public:
	VTableSpec(const OString &name, int methodCount);

	const OString &GetName() const { return m_name; }
	int GetMethodCount() const { return m_methods.size(); }
	VMethodSpec &GetMethodByIndex(int index) { return m_methods[index]; }

	VMethodSpec &operator[](int index) { return m_methods[index]; }

protected:
	OString m_name;
	OVector<VMethodSpec>::Type m_methods;
};

typedef void (*VMethodHandler) (VMethod *method, va_list args);

class VMethodSpec : public BaseObject
{
public:
	VMethodSpec() : m_handler(NULL) {}
	void Initialize(VTableSpec *vtable, int index);

	const VTableSpec *GetVTable() { return m_vtable; }
	int GetIndex() { return m_index; }
	const OString &GetName() { return m_name; }
	void SetName(const OString &name) { m_name = name; }
	void SetHandler(VMethodHandler handler) { m_handler = handler; }

protected:
	VTableSpec *m_vtable;
	int m_index;
	OString m_name;
	VMethodHandler m_handler;
};

class VTable : public BaseObject
{
public:
	VTable(VTableSpec *spec, DWORD startOffset);

	const OString &GetName() { return m_name; }
	VTableSpec *GetSpec() { return m_spec; }
	DWORD GetStartOffset() { return m_startOffset; }
	VMethod &GetMethodByIndex(int index) { return m_methods[index]; }

	void Hook();

	VMethod &operator[](int index) { return m_methods[index]; }

protected:
	OString m_name;
	VTableSpec *m_spec;
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

private:
	static void OnEnterProxy(CpuContext cpuCtx, VMethod *method);
	VMethodTrampoline *OnEnterWrapper(void *retAddr, CpuContext *cpuCtx);
	void OnEnter(VMethodCall *call);

	static void OnLeaveProxy(CpuContext cpuCtx, VMethodCall *call);
	void OnLeave(VMethodCall *call);
};

class VMethodCall : public BaseObject
{
public:
	VMethodCall(VMethod *method=NULL, void *retAddr=NULL,
				CpuContext *cpuCtxEnter=NULL, CpuContext *cpuCtxLeave=NULL)
		: m_method(method), m_returnAddress(retAddr)
	{
		if (cpuCtxEnter != NULL)
			m_cpuCtxEnter = *cpuCtxEnter;
		if (cpuCtxLeave != NULL)
			m_cpuCtxLeave = *cpuCtxLeave;
	}

	VMethodCall(const VMethodCall &methodCall)
	{
		m_method = methodCall.m_method;
		m_returnAddress = methodCall.m_returnAddress;
		m_cpuCtxEnter = methodCall.m_cpuCtxEnter;
		m_cpuCtxLeave = methodCall.m_cpuCtxLeave;
	}

	VMethod *GetMethod() const { return m_method; }
	void *GetReturnAddress() const { return m_returnAddress; }
	const CpuContext *GetCpuContextEnter() const { return &m_cpuCtxEnter; }
	const CpuContext *GetCpuContextLeave() const { return &m_cpuCtxLeave; }
	void SetCpuContextLeave(const CpuContext *ctx) { m_cpuCtxLeave = *ctx; }

protected:
	VMethod *m_method;
	void *m_returnAddress;
	CpuContext m_cpuCtxEnter;
	CpuContext m_cpuCtxLeave;
};
