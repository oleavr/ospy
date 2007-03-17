//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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

#include "byte_buffer.h"
#include <map>

template <class T>
class ContextTracker
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

    typedef map<T, DWORD, less<T>, MyAlloc<pair<T, DWORD>>> ContextMap;
    ContextMap contexts;
};

void get_process_name(char *name, int len);
void get_module_name_for_address(LPVOID address, char *buf, int buf_size);
BOOL get_module_base_and_size(const char *module_name, LPVOID *base, DWORD *size, char **error);
BOOL address_has_bytes(LPVOID address, unsigned char *buf, int len);
bool cur_process_is(const char *name);

DWORD ospy_rand();