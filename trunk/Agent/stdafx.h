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

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN		// Exclude rarely-used stuff from Windows headers
#endif

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
#ifndef WINVER				// Allow use of features specific to Windows XP or later.
#define WINVER 0x0501		// Change this to the appropriate value to target other versions of Windows.
#endif

#ifndef _WIN32_WINNT		// Allow use of features specific to Windows XP or later.                   
#define _WIN32_WINNT 0x0501	// Change this to the appropriate value to target other versions of Windows.
#endif						

#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
#define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
#endif

#ifndef _WIN32_IE			// Allow use of features specific to IE 6.0 or later.
#define _WIN32_IE 0x0600	// Change this to the appropriate value to target other versions of IE.
#endif

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxole.h>         // MFC OLE classes
#include <afxodlgs.h>       // MFC OLE dialog classes
#include <afxdisp.h>        // MFC Automation classes
#endif // _AFX_NO_OLE_SUPPORT

#ifndef _AFX_NO_DB_SUPPORT
#include <afxdb.h>			// MFC ODBC database classes
#endif // _AFX_NO_DB_SUPPORT

#ifndef _AFX_NO_DAO_SUPPORT
#include <afxdao.h>			// MFC DAO database classes
#endif // _AFX_NO_DAO_SUPPORT

#ifndef _AFX_NO_OLE_SUPPORT
#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
#endif
#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h>			// MFC support for Windows Common Controls
#endif // _AFX_NO_AFXCMN_SUPPORT

#include <stdlib.h>
#include <stdarg.h>
#include <winsock2.h>

#include <map>

template<class TYPE> class safe_allocator : public std::allocator<TYPE>

{
	typedef std::allocator<TYPE> parent_type;

public:
	safe_allocator() throw() : parent_type() {}
	safe_allocator(const allocator &c) throw() : parent_type(c) {}

	pointer allocate(size_type n, const void* hint = 0)
	{
		return (pointer)HeapAlloc(GetProcessHeap(), 0, n * sizeof(value_type));
	}

	void deallocate(pointer p, size_type n)
	{
		operator HeapFree(GetProcessHeap(), 0, p);
	}

	char *_Charalloc(size_type n)
	{
		return (char*)HeapAlloc(GetProcessHeap(), 0, n);
	}
};

/*
inline void * operator new(size_t amt)  { return HeapAlloc(GetProcessHeap(), 0, amt); }
inline void operator delete(void* ptr)  { HeapFree(GetProcessHeap(), 0, ptr); }
inline void *operator new[](size_t amt)  { return operator new(amt); }
inline void operator delete[](void* ptr)  { operator delete(ptr); }
*/

void *sspy_malloc(size_t size);
void *sspy_realloc(void *ptr, size_t new_size);
void sspy_free(void *ptr);
char *sspy_strdup(const char *str);

#define HOOK_ACTIVESYNC   1
#define FILTERING_ENABLED 0

#pragma warning(disable: 4311 4312 4996)
