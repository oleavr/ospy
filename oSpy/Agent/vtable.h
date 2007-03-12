#pragma once

#include "hooking.h"

typedef struct {
	DWORD index;
	DWORD functionStart;
} VFuncContext;

class VTableSpec;
class VMethodSpec;

class VTableHooker : public BaseObject
{
public:
	static VTableHooker *Self();

	void HookVTable(VTableSpec &vtable);

protected:
	static void VTableProxyFunc(CpuContext cpuCtx, VMethodSpec *methodSpec);
};

class VTableSpec : public BaseObject
{
public:
	VTableSpec(const OString &name, DWORD startOffset, int methodCount);

	const OString &GetName() { return m_name; }
	DWORD GetStartOffset() { return m_startOffset; }
	int GetMethodCount() { return m_methods.size(); }
	VMethodSpec *GetMethodByIndex(int index) { return &m_methods[index]; }

	VMethodSpec &operator[](int index) { return m_methods[index]; }
protected:
	OString m_name;
	DWORD m_startOffset;
	OVector<VMethodSpec>::Type m_methods;
};

class VMethodSpec : public BaseObject
{
public:
	void Initialize(VTableSpec *vtable, int index, DWORD offset);

	const VTableSpec *GetVTable() { return m_vtable; }
	int GetIndex() { return m_index; }
	DWORD GetOffset() { return m_offset; }
	const OString &GetName() { return m_name; }
	void SetName(const OString &name) { m_name = name; }

protected:
	VTableSpec *m_vtable;
	int m_index;
	DWORD m_offset;
	OString m_name;
};
