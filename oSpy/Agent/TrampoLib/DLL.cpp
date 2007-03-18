//
// Copyright (c) 2007 Ole Andr� Vadla Ravn�s <oleavr@gmail.com>
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

#include "stdafx.h"
#include "DLL.h"
#include "Util.h"

namespace TrampoLib {

DllModule::DllModule(const OString &path, const OString &name)
    : m_path(path), m_name(name)
{
    m_handle = LoadLibraryA(path.c_str());
    if (m_handle == NULL)
        throw runtime_error("LoadLibrary failed");

    OModuleInfo mi = Util::GetModuleInfo(name.c_str());
    m_base = reinterpret_cast<void *>(mi.startAddress);
    m_size = mi.endAddress - mi.startAddress;
}

void *
DllModule::FindUniqueSignature(const Signature *sig)
{
    SignatureMatcher *sm = SignatureMatcher::Instance();

    OVector<void *>::Type matches = sm->FindInRange(sig, m_base, m_size);
    if (matches.size() == 0)
        throw runtime_error("no matches found");
    else if (matches.size() > 1)
        throw runtime_error("more than one match found");

    return matches[0];
}

DllFunction::DllFunction(DllModule *module, FunctionSpec *spec)
    : m_module(module)
{
    FARPROC offset = GetProcAddress(module->GetHandle(), spec->GetName().c_str());
    if (offset == NULL)
        throw runtime_error("GetProcAddress failed");

    Function::Initialize(spec, reinterpret_cast<DWORD>(offset));
}


} // namespace TrampoLib