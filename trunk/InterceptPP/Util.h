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
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#pragma once

#include "Core.h"
#include "DLL.h"
#include "Logging.h"

namespace InterceptPP {

typedef struct {
    HMODULE handle;
	OICString name;
    OString directory;
	DWORD preferredStartAddress;
	DWORD startAddress;
	DWORD endAddress;
} OModuleInfo;

class Util : public BaseObject
{
public:
    Util();

    static Util *Instance();

    void Initialize();
    void UnInitialize();
	void UpdateModuleList();

	const OString &GetProcessName() { return m_processName; }
	OModuleInfo GetModuleInfo(const OICString &name);
    OModuleInfo GetModuleInfo(void *address);
	OVector<OModuleInfo>::Type GetAllModules();
	bool AddressIsWithinAnyModule(DWORD address);
	bool AddressIsWithinModule(DWORD address, const OICString &moduleName);

    OString GetDirectory(const OModuleInfo &mi);

    Logging::Node *CreateBacktraceNode(void *address);
	OString CreateBacktrace(void *address);

private:
    static bool OnLoadLibrary(FunctionCall *call);
	OModuleInfo *GetModuleInfoForAddress(DWORD address);

	CRITICAL_SECTION m_cs;
	OString m_processName;

    FunctionSpec *m_asciiFuncSpec;
    FunctionSpec *m_uniFuncSpec;
    DllModule *m_mod;
    DllFunction *m_asciiFunc;
    DllFunction *m_uniFunc;

	OMap<OICString, OModuleInfo>::Type m_modules;
	DWORD m_lowestAddress;
	DWORD m_highestAddress;
};

} // namespace InterceptPP