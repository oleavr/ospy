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

#include "stdafx.h"
#include "DLL.h"
#include "Util.h"
#include <psapi.h>

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

DllModule::DllModule(const OString &path)
    : m_path(path)
{
    m_handle = LoadLibraryA(path.c_str());
    if (m_handle == NULL)
        throw Error("LoadLibrary failed");

    char tmp[_MAX_PATH];
    if (GetModuleBaseNameA(GetCurrentProcess(), m_handle, tmp, sizeof(tmp)) == 0)
        throw Error("GetModuleBaseName failed");

    m_name = tmp;

    OModuleInfo mi = Util::Instance()->GetModuleInfo(m_name.c_str());
    m_base = reinterpret_cast<void *>(mi.startAddress);
    m_size = mi.endAddress - mi.startAddress;
}

DllModule::~DllModule()
{
    if (m_handle != NULL)
        FreeLibrary(m_handle);
}

void *
DllModule::FindUniqueSignature(const Signature *sig)
{
    SignatureMatcher *sm = SignatureMatcher::Instance();

    OVector<void *>::Type matches = sm->FindInRange(*sig, m_base, m_size);
    if (matches.size() == 0)
        throw Error("no matches found");
    else if (matches.size() > 1)
        throw Error("more than one match found");

    return matches[0];
}

DllFunction::DllFunction(DllModule *module, FunctionSpec *spec)
    : m_module(module)
{
    FARPROC offset = GetProcAddress(module->GetHandle(), spec->GetName().c_str());
    if (offset == NULL)
        throw Error("GetProcAddress failed");

    Function::Initialize(spec, reinterpret_cast<DWORD>(offset));
}

} // namespace InterceptPP
