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

namespace TrampoLib {

typedef struct {
	OICString name;
	DWORD preferredStartAddress;
	DWORD startAddress;
	DWORD endAddress;
} OModuleInfo;

class Util
{
public:
	static void Initialize();
	static void UpdateModuleList();

	static const OString &GetProcessName() { return m_processName; }
	static OString GetModuleNameForAddress(DWORD address);
	static OModuleInfo GetModuleInfo(const OICString &name);
	static OVector<OModuleInfo>::Type GetAllModules();
	static bool AddressIsWithinExecutableModule(DWORD address);

	static OString CreateBackTrace(void *address);

private:
	static OModuleInfo *GetModuleInfoForAddress(DWORD address);

	static CRITICAL_SECTION m_cs;
	static OString m_processName;
	static OMap<OICString, OModuleInfo>::Type m_modules;
	static DWORD m_lowestAddress;
	static DWORD m_highestAddress;
};

} // namespace TrampoLib