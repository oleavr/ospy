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

#include <winsock2.h>

#define SOFTWALL_CONDITION_PROCESS_NAME    1
#define SOFTWALL_CONDITION_FUNCTION_NAME   2
#define SOFTWALL_CONDITION_RETURN_ADDRESS  4
#define SOFTWALL_CONDITION_LOCAL_ADDRESS   8
#define SOFTWALL_CONDITION_LOCAL_PORT     16
#define SOFTWALL_CONDITION_REMOTE_ADDRESS 32
#define SOFTWALL_CONDITION_REMOTE_PORT    64

typedef struct {
    /* mask of conditions */
    DWORD conditions;

    /* condition values */
    char process_name[32];
    char function_name[16];
    DWORD return_address;
    in_addr local_address;
    int local_port;
    in_addr remote_address;
    int remote_port;

    /* return value and lasterror to set if all conditions match */
    int retval;
    DWORD last_error;
} SoftwallRule;

DWORD softwall_decide_from_addresses(const char *function_name, DWORD return_address, const sockaddr_in *local_address, const sockaddr_in *remote_address, BOOL *carry_on);
DWORD softwall_decide_from_socket(const char *function_name, DWORD return_address, SOCKET s, BOOL *carry_on);
DWORD softwall_decide_from_socket_and_remote_address(const char *function_name, DWORD return_address, SOCKET s, const sockaddr_in *remote_address, BOOL *carry_on);
