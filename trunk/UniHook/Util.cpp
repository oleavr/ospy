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
#include "Util.h"
#include <Psapi.h>
#include <iostream>
#include <sstream>
#include <iomanip>

void *
ospy_malloc(size_t size)
{
    return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, size);
}

void *
ospy_realloc(void *ptr, size_t new_size)
{
    return HeapReAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, ptr, new_size);
}

void
ospy_free(void *ptr)
{
    HeapFree(GetProcessHeap(), 0, ptr);
}

char *
ospy_strdup(const char *str)
{
    char *s;
    size_t size = strlen(str) + 1;

    s = (char *) ospy_malloc(size);
    memcpy(s, str, size);

    return s;
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
                GetModuleBaseNameA(process, modules[i], buf, buf_size);
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
            *error = ospy_strdup("EnumProcessModules failed");
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

            if (GetModuleBaseNameA(process, modules[i], buf, 32) == 0)
            {
                if (error)
                    *error = ospy_strdup("GetModuleBaseName failed");
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
        *error = ospy_strdup("module not found");
    return FALSE;
}

// Thanks to Einar Otto Stangvik (http://einaros.livejournal.com/) for
// posting this neat little snippet in his blog

std::string hexdump(void *x, unsigned long len, unsigned int w)
{
	std::ostringstream osDump;
	std::ostringstream osNums;
	std::ostringstream osChars;
	std::string szPrevNums;
	bool bRepeated = false;
	unsigned long i;

	for(i = 0; i <= len; i++) 
	{ 
		if(i < len) 
		{ 
			unsigned char c = *((unsigned char *) x + i); 
			unsigned int n = (unsigned int)*((unsigned char*)x + i); 
			osNums << std::setbase(16) << std::setw(2) << std::setfill('0') << n << " "; 
			if(((i % w) != w - 1) && ((i % w) % 8 == 7)) 
			osNums << "- ";
			osChars << ((c > 127 || iscntrl(c)) ? '.' : (char) c); 
		}

		if(osNums.str().compare(szPrevNums) == 0) 
		{ 
			bRepeated = true; 
			osNums.str(""); 
			osChars.str(""); 
			if (i == len - 1) 
				osDump << "*" << std::endl; 
			continue; 
		} 

		if(((i % w) == w - 1) || ((i == len) && (osNums.str().size() > 0))) 
		{ 
			if(bRepeated) 
			{ 
				osDump << "*" << std::endl; 
				bRepeated = false; 
			} 
			osDump << std::setbase(16) << std::setw(8) << std::setfill('0') << (i - (i % w)) << "  " 
			   << std::setfill(' ') << std::setiosflags(std::ios_base::left) 
			   << std::setw(3 * w + ((w / 8) - 1) * 2) << osNums.str() 
			   << " |" << osChars.str() << std::resetiosflags(std::ios_base::left) << "|" << std::endl; 
			szPrevNums = osNums.str(); 
			osNums.str(""); 
			osChars.str(""); 
		} 
	}

	osDump << std::setbase(16) << std::setw(8) << std::setfill('0') << (i-1) << std::endl; 

	return osDump.str(); 
}
