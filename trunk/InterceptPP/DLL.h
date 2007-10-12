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

class DllModule : public BaseObject {
public:
    DllModule(const OString &path);
    ~DllModule();

    const OString &GetPath() const { return m_path; }
    const OString &GetName() const { return m_name; }
    HMODULE GetHandle() const { return m_handle; }

    void *FindUniqueSignature(const Signature *sig);

protected:
    OString m_path;
    OString m_name;
    HMODULE m_handle;
    void *m_base;
    DWORD m_size;
};

class DllFunction : public Function
{
public:
    DllFunction(DllModule *module, FunctionSpec *spec);

    virtual const OString GetParentName() const { return m_module->GetName(); }

    DllModule *GetModule() const { return m_module; }

protected:
    DllModule *m_module;
};

} // namespace InterceptPP
