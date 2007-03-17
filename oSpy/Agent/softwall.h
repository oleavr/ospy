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