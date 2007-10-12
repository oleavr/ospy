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

private:
	static void OnLoadLibrary(FunctionCall *call, void *userData, bool &shouldLog);

	OModuleInfo *GetModuleInfoForAddress(DWORD address);

	CRITICAL_SECTION m_cs;
	OString m_processName;

    FunctionSpec *m_asciiFuncSpec;
    FunctionSpec *m_uniFuncSpec;
    DllModule *m_mod;
    DllFunction *m_asciiFunc;
    DllFunction *m_uniFunc;

	OMap<OICString, OModuleInfo>::Type m_modules;
	volatile DWORD m_lowestAddress;
	volatile DWORD m_highestAddress;
};

} // namespace InterceptPP
