/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

#include "stdafx.h"
#include "util.h"
#include "logging.h"
#include <psapi.h>

static bool initialized = FALSE;
static char cur_process_name[_MAX_PATH];

bool
cur_process_is(const char *name)
{
  if (!initialized)
  {
    initialized = TRUE;

    // Get the current process name
    if (GetModuleBaseName(GetCurrentProcess(), NULL, cur_process_name, _MAX_PATH) == 0)
    {
      message_logger_log_message("DllMain", 0, MESSAGE_CTX_WARNING,
                                 "GetModuleBaseName failed with errno %d",
                                 GetLastError());
      return FALSE;
    }
  }

  return (stricmp(cur_process_name, name) == 0);
}

void
get_module_name_for_address(LPVOID address,
                            char *buf, int buf_size)
{
    HANDLE process;
    HMODULE modules[256];
    DWORD bytes_needed, num_modules;
    unsigned int i;

    buf[0] = '\0';

    process = GetCurrentProcess();

    if (EnumProcessModules(process, (HMODULE *) &modules,
                           sizeof(modules), &bytes_needed) == 0)
    {
        return;
    }

    if (bytes_needed > sizeof(modules))
        bytes_needed = sizeof(modules);

    num_modules = bytes_needed / sizeof(HMODULE);

    for (i = 0; i < num_modules; i++)
    {
        MODULEINFO mi;

        if (GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
        {
            LPVOID start, end;

            start = mi.lpBaseOfDll;
            end = (char *) start + mi.SizeOfImage;

            if (address >= start && address <= end)
            {
                GetModuleBaseName(process, modules[i], buf, buf_size);
                return;
            }
        }
    }
}

BOOL
get_module_base_and_size(const char *module_name, LPVOID *base, DWORD *size, char **error)
{
    HANDLE process;
    HMODULE modules[256];
    DWORD bytes_needed, num_modules;
    unsigned int i;

    process = GetCurrentProcess();

    if (EnumProcessModules(process, (HMODULE *) &modules,
                           sizeof(modules), &bytes_needed) == 0)
    {
        if (error)
            *error = sspy_strdup("EnumProcessModules failed");
        return FALSE;
    }

    if (bytes_needed > sizeof(modules))
        bytes_needed = sizeof(modules);

    num_modules = bytes_needed / sizeof(HMODULE);

    for (i = 0; i < num_modules; i++)
    {
        MODULEINFO mi;

        if (GetModuleInformation(process, modules[i], &mi, sizeof(mi)) != 0)
        {
            char buf[32];
            LPVOID start, end;

            start = mi.lpBaseOfDll;
            end = (char *) start + mi.SizeOfImage;

            if (GetModuleBaseName(process, modules[i], buf, 32) == 0)
            {
                if (error)
                    *error = sspy_strdup("GetModuleBaseName failed");
                return FALSE;
            }

            if (stricmp(buf, module_name) == 0)
            {
                *base = mi.lpBaseOfDll;
                *size = mi.SizeOfImage;

                return TRUE;
            }
        }
    }

    if (error)
        *error = sspy_strdup("module not found");
    return FALSE;
}

BOOL address_has_bytes(LPVOID address, unsigned char *buf, int len)
{
  unsigned char *func_bytes = (unsigned char *) address;
  int i;

  for (i = 0; i < len; i++)
  {
    if (func_bytes[i] != buf[i])
    {
      message_logger_log_message("address_has_bytes", 0, MESSAGE_CTX_ERROR,
                                 "Signature mismatch on index %d, expected %d got %d",
                                 i, buf[i], func_bytes[i]);
      return FALSE;
    }
  }

  return TRUE;
}

DWORD
ospy_rand()
{
    LARGE_INTEGER seed;

    QueryPerformanceCounter(&seed);
    srand(seed.LowPart);

    return 1 + rand();
}

void
ORPCBuffer::AppendString(const OString &str)
{
	unsigned short val = (unsigned short) str.size();
	append((char *) &val, sizeof(val));
	append(str.c_str(), str.size());
}

void
ORPCBuffer::AppendData(void *data, unsigned short len)
{
	append((char *) &len, sizeof(len));
	append((char *) data, len);
}

void
ORPCBuffer::AppendDWORD(DWORD dw)
{
	append((char *) &dw, sizeof(dw));
}

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
