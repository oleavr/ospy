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

#if 0

static bool filter_google_relay(const char *function_name,
                                DWORD ret_addr,
                                const struct sockaddr *dest_addr,
                                int *retval)
{
  const struct sockaddr_in *sin = (const struct sockaddr_in *) dest_addr;
  const char *addr_str = inet_ntoa(sin->sin_addr);
  int port = ntohs(sin->sin_port);

  if (ret_addr == 0x47eb7f || ret_addr == 0x47eacd)
  {
    message_logger_log_message(function_name, ret_addr, MESSAGE_CTX_WARNING,
                               "rejecting GTalk UDP candidate traffic with %s",
                               addr_str);

    SetLastError(WSAEHOSTUNREACH);
    *retval = SOCKET_ERROR;

    return true;
  }

  return false;
}

/*

  if (port != 5222
      &&
      (
         (strcmp(addr_str, "64.233.167.126") == 0 ||
          strcmp(addr_str, "216.239.37.125") == 0 ||
          strcmp(addr_str, "216.239.37.126") == 0)
       ||
      ))
*/

#endif