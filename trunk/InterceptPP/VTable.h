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

#include "Core.h"

namespace InterceptPP {

class VMethodSpec;
class VMethod;

class VTableSpec : public BaseObject
{
public:
    VTableSpec(const OString &name, int methodCount);

    const OString &GetName() const { return m_name; }
    unsigned int GetMethodCount() const { return static_cast<unsigned int>(m_methods.size()); }
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
    void UnHook();

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

    void StealFrom(FunctionSpec *funcSpec);

protected:
    VTableSpec *m_vtable;
    int m_index;
};

class VMethod : public Function
{
public:
    VMethod(VTable *vtable=NULL, FunctionSpec *spec=NULL, DWORD offset=0)
    {
        Initialize(vtable, spec, offset);
    }

    void Initialize(VTable *vtable, FunctionSpec *spec, DWORD offset)
    {
        Function::Initialize(spec, offset);
        m_vtable = vtable;
    }

    virtual const OString GetParentName() const { return m_vtable->GetName(); }

    VTable *GetVTable() const { return m_vtable; }

protected:
    VTable *m_vtable;
};

} // namespace InterceptPP
