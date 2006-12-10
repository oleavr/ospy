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

#pragma once

#include "byte_buffer.h"
#include <vector>
#include <map>
#include <string>

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

struct ci_char_traits : public char_traits<char>
            // just inherit all the other functions
            //  that we don't need to override
{
	static bool eq(char c1, char c2)
	{
		return toupper(c1) == toupper(c2);
	}

	static bool ne(char c1, char c2)
	{
		return toupper(c1) != toupper(c2);
	}

	static bool lt(char c1, char c2)
	{
		return toupper(c1) <  toupper(c2);
	}

	static int compare(const char *s1, const char *s2, size_t n)
	{
		return memicmp(s1, s2, n);
	}

	static const char *find(const char *s, int n, char a)
	{
		while(n-- > 0 && toupper(*s) != toupper(a)) {
			++s;
		}

		return s;
	}
};

typedef basic_string<char, char_traits<char>, MyAlloc<char>> OString;
typedef basic_string<wchar_t, char_traits<wchar_t>, MyAlloc<wchar_t>> OWString;

typedef basic_string<char, ci_char_traits, MyAlloc<char>> OICString;

template <class eT>
struct OVector
{
	typedef std::vector<eT, MyAlloc<eT>> Type;
};

template <class kT, class vT>
struct OMap
{
	typedef std::map<kT, vT, std::less<kT>, MyAlloc<std::pair<kT, vT>>> Type;
};

class ORPCBuffer : public OString
{
public:
	void AppendString(const OString &str);
	void AppendData(void *data, unsigned short len);
	void AppendDWORD(DWORD dw);
};

typedef struct {
	OICString name;
	void *startAddress;
	void *endAddress;
} OModuleInfo;

class CUtil
{
public:
	static void Init();

	static const OString &GetProcessName() { return m_processName; }
	static OString GetModuleNameForAddress(LPVOID address);
	static OModuleInfo GetModuleInfo(const OICString &name) { return m_modules[name]; }
	static OVector<OModuleInfo>::Type GetAllModules();

private:
	static void UpdateModuleList();

	static OString m_processName;
	static OMap<OICString, OModuleInfo>::Type m_modules;
};

void get_module_name_for_address(LPVOID address, char *buf, int buf_size);
BOOL get_module_base_and_size(const char *module_name, LPVOID *base, DWORD *size, char **error);
BOOL address_has_bytes(LPVOID address, unsigned char *buf, int len);
bool cur_process_is(const char *name);

DWORD ospy_rand();