//
// Copyright (c) 2006-2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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

#include "stdafx.h"
#include "logging_old.h"
#include "hooking.h"
#include "util.h"
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

void get_process_name(char *name, int len)
{
  WCHAR path_buffer[_MAX_PATH];
  WCHAR drive[_MAX_DRIVE];
  WCHAR dir[_MAX_DIR];
  WCHAR fname[_MAX_FNAME];
  WCHAR ext[_MAX_EXT];

  GetModuleFileNameW(NULL, path_buffer, _MAX_PATH);

  _wsplitpath_s(path_buffer, drive, _MAX_DRIVE, dir, _MAX_DIR,
                fname, _MAX_FNAME, ext, _MAX_EXT);

  wsprintf(name, "%S%S", fname, ext);
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
