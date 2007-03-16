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
// FROM, OUT OF OR IN CONNECTION WI
//

#pragma once

#include "Core.h"

namespace TrampoLib {

class VMethodSpec;
class VMethod;

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

class VMethodSpec : public FunctionSpec
{
public:
	VMethodSpec()
		: m_vtable(NULL), m_index(-1)
	{}

    void Initialize(VTableSpec *vtable, int index);

    const VTableSpec *GetVTable() { return m_vtable; }
	int GetIndex() const { return m_index; }

protected:
	VTableSpec *m_vtable;
	int m_index;
};

class VMethod : public Function
{
public:

    void Initialize(FunctionSpec *spec, DWORD offset, VTable *vtable)
    {
        Function::Initialize(spec, offset);
        m_vtable = vtable;
    }

    virtual const OString &GetParentName() const { return m_vtable->GetName(); }

	VTable *GetVTable() const { return m_vtable; }

protected:
	VTable *m_vtable;
};

} // namespace TrampoLib