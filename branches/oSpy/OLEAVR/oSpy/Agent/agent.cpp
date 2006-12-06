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
#include "logging.h"
#include "hooks.h"

#ifdef _MANAGED
#pragma managed(push, off)
#endif

void *
sspy_malloc(size_t size)
{
    return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, size);
}

void *
sspy_realloc(void *ptr, size_t new_size)
{
    return HeapReAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, ptr, new_size);
}

void
sspy_free(void *ptr)
{
    HeapFree(GetProcessHeap(), 0, ptr);
}

char *
sspy_strdup(const char *str)
{
    char *s;
    size_t size = strlen(str) + 1;

    s = (char *) sspy_malloc(size);
    memcpy(s, str, size);

    return s;
}


BOOL APIENTRY
DllMain(HMODULE hModule,
        DWORD  ul_reason_for_call,
        LPVOID lpReserved)
{
    if (ul_reason_for_call == DLL_PROCESS_ATTACH)
    {
        // Initialize SHM logger
        message_logger_init();

        hook_winsock();
        hook_secur32();
        hook_crypt();
        //hook_httpapi();
        hook_activesync();
        hook_msn();
    }

    return TRUE;
}

#ifdef _MANAGED
#pragma managed(pop)
#endif