//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

#pragma once

#include "byte_buffer.h"
#include <map>

template <class T>
class ContextTracker : public BaseObject
{
public:
    ContextTracker()
    {
        InitializeCriticalSection(&cs);
    }

    ~ContextTracker()
    {
    }

    DWORD GetContextID(T handle)
    {
        DWORD id;

        EnterCriticalSection(&cs);

        ContextMap::iterator iter = contexts.find(handle);
        if (iter != contexts.end())
        {
            id = iter->second;
        }
        else
        {
            id = ospy_rand();
            contexts[handle] = id;
        }

        LeaveCriticalSection(&cs);

        return id;
    }

    void RemoveContextID(T handle)
    {
        EnterCriticalSection(&cs);

        ContextMap::iterator iter = contexts.find(handle);
        if (iter != contexts.end())
        {
            contexts.erase(iter);
        }

        LeaveCriticalSection(&cs);
    }

private:
    CRITICAL_SECTION cs;

    typedef std::map<T, DWORD, std::less<T>, MyAlloc<std::pair<T, DWORD>>> ContextMap;
    //typedef OMap<T, DWORD>::Type ContextMap;
    ContextMap contexts;
};

void get_process_name(char *name, int len);
void get_module_name_for_address(LPVOID address, char *buf, int buf_size);
BOOL get_module_base_and_size(const char *module_name, LPVOID *base, DWORD *size, char **error);
BOOL address_has_bytes(LPVOID address, unsigned char *buf, int len);
bool cur_process_is(const char *name);

DWORD ospy_rand();
char *ospy_strdup(const char *str);
