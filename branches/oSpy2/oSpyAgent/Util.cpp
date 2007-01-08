//
// Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

OString CUtil::m_processName = "";
OMap<OICString, OModuleInfo>::Type CUtil::m_modules;

void
CUtil::Init()
{
	char buf[_MAX_PATH];
	if (GetModuleBaseNameA(NULL, NULL, buf, sizeof(buf)) > 0)
	{
		m_processName = buf;
	}

	UpdateModuleList();
}

void
CUtil::UpdateModuleList()
{
	m_modules.clear();

    HANDLE process = GetCurrentProcess();

    HMODULE modules[256];
    DWORD bytes_needed;

    if (EnumProcessModules(process, (HMODULE *) &modules,
                           sizeof(modules), &bytes_needed) == 0)
    {
        return;
    }

    if (bytes_needed > sizeof(modules))
        bytes_needed = sizeof(modules);

    for (unsigned int i = 0; i < bytes_needed / sizeof(HMODULE); i++)
    {
		char buf[128];

		if (GetModuleBaseNameA(process, modules[i], buf, sizeof(buf)) != 0)
		{
			MODULEINFO mi;
			if (GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
			{
				OModuleInfo modInfo;
				modInfo.name = buf;
				modInfo.startAddress = mi.lpBaseOfDll;
				modInfo.endAddress = (void *) ((DWORD) mi.lpBaseOfDll + mi.SizeOfImage - 1);
				m_modules[buf] = modInfo;
			}
		}
    }
}

OString
CUtil::GetModuleNameForAddress(LPVOID address)
{
	OMap<OICString, OModuleInfo>::Type::iterator it;
	for (it = m_modules.begin(); it != m_modules.end(); it++)
	{
		OModuleInfo &mi = (*it).second;

		if (address >= mi.startAddress && address <= mi.endAddress)
		{
			return mi.name.c_str();
		}
	}

	return "";
}

OVector<OModuleInfo>::Type
CUtil::GetAllModules()
{
	OVector<OModuleInfo>::Type ret;

	OMap<OICString, OModuleInfo>::Type::iterator it;
	for (it = m_modules.begin(); it != m_modules.end(); it++)
	{
		ret.push_back((*it).second);
	}

	return ret;
}
