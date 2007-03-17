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

class DllModule : BaseObject {
public:
    DllModule(const OString &path, const OString &name)
        : m_path(path), m_name(name)
    {
        m_handle = LoadLibrary(path.c_str());
        if (m_handle == NULL)
            throw runtime_error("LoadLibrary failed");
    }

    const OString &GetPath() const { return m_path; }
    const OString &GetName() const { return m_name; }
    HMODULE GetHandle() const { return m_handle; }

protected:
    OString m_path;
    OString m_name;
    HMODULE m_handle;
};

class DllFunction : public Function
{
public:
    DLLFunction(DllModule *module, FunctionSpec *spec)
        : m_module(module), 
    {
        void *offset = GetProcAddress(module->GetHandle(), spec->GetName());
        if (offset == NULL)
            throw runtime_error("GetProcAddress failed");

        Function::Initialize(spec, offset);

        m_module = module;
        m_name = name;
    }

    virtual const OString &GetParentName() const { return m_module->GetName(); }

    DllModule *GetModule() const { return m_module; }

protected:
    DllModule *m_module;
};

} // namespace TrampoLib