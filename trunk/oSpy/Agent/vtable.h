#pragma once

#include "hooking.h"

class VTableSpec;
class VMethodSpec;
class VTableInstance;
class VMethodInstance;

class VTableHooker : public BaseObject
{
public:
	static VTableHooker *Self();

	void HookVTable(VTableInstance *vtable);

protected:
	static void VTableProxyFunc(CpuContext cpuCtx, VMethodInstance *method);
};

class VTableSpec : public BaseObject
{
public:
	VTableSpec(const OString &name, int methodCount);

	const OString &GetName() { return m_name; }
	int GetMethodCount() { return m_methods.size(); }
	VMethodSpec &GetMethodByIndex(int index) { return m_methods[index]; }

	VMethodSpec &operator[](int index) { return m_methods[index]; }

protected:
	OString m_name;
	OVector<VMethodSpec>::Type m_methods;
};

class VMethodSpec : public BaseObject
{
public:
	void Initialize(VTableSpec *vtable, int index);

	const VTableSpec *GetVTable() { return m_vtable; }
	int GetIndex() { return m_index; }
	const OString &GetName() { return m_name; }
	void SetName(const OString &name) { m_name = name; }

protected:
	VTableSpec *m_vtable;
	int m_index;
	OString m_name;
};

class VTableInstance : public BaseObject
{
public:
	VTableInstance(VTableSpec *spec, DWORD startOffset);

	const OString &GetName() { return m_name; }
	VTableSpec *GetSpec() { return m_spec; }
	DWORD GetStartOffset() { return m_startOffset; }
	VMethodInstance &GetMethodByIndex(int index) { return m_methods[index]; }

	VMethodInstance &operator[](int index) { return m_methods[index]; }

protected:
	OString m_name;
	VTableSpec *m_spec;
	DWORD m_startOffset;
	OVector<VMethodInstance>::Type m_methods;
};

class VMethodInstance : public BaseObject
{
public:
	void Initialize(VMethodSpec *spec, VTableInstance *vtable, DWORD offset) { m_spec = spec; m_vtable = vtable; m_offset = offset; }

	VMethodSpec *GetSpec() { return m_spec; }
	VTableInstance *GetVTable() { return m_vtable; }
	DWORD GetOffset() { return m_offset; }

protected:
	VMethodSpec *m_spec;
	VTableInstance *m_vtable;
	DWORD m_offset;
};
