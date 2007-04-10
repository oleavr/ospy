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

#include "stdafx.h"
#include "logging_old.h"
#include "util.h"

extern MessageQueue *queue;
extern HANDLE queue_mutex;

static BOOL
softwall_find_rule(const char *function_name,
                   DWORD return_address,
                   const sockaddr_in *local_address,
                   const sockaddr_in *remote_address,
                   int *rule_retval,
                   DWORD *rule_last_error)
{
    if (WaitForSingleObject(queue_mutex, INFINITE) != WAIT_OBJECT_0)
        return FALSE;

    __try
    {
        for (int i = 0; i < queue->num_softwall_rules; i++)
        {
            SoftwallRule *rule = &queue->rules[i];

            if ((rule->conditions & SOFTWALL_CONDITION_PROCESS_NAME) != 0)
            {
                if (!cur_process_is(rule->process_name))
                    continue;
            }

            if ((rule->conditions & SOFTWALL_CONDITION_FUNCTION_NAME) != 0)
            {
                if (strcmp(function_name, rule->function_name) != 0)
                    continue;
            }

            if ((rule->conditions & SOFTWALL_CONDITION_RETURN_ADDRESS) != 0)
            {
                if (return_address != rule->return_address)
                    continue;
            }
            
            if ((rule->conditions & SOFTWALL_CONDITION_LOCAL_ADDRESS) != 0)
            {
                if (local_address == NULL ||
                    memcmp(&local_address->sin_addr, &rule->local_address, sizeof(in_addr)) != 0)
                {
                    continue;
                }
            }

            if ((rule->conditions & SOFTWALL_CONDITION_LOCAL_PORT) != 0)
            {
                if (local_address == NULL ||
                    local_address->sin_port != rule->local_port)
                {
                    continue;
                }
            }

            if ((rule->conditions & SOFTWALL_CONDITION_REMOTE_ADDRESS) != 0)
            {
                if (remote_address == NULL ||
                    memcmp(&remote_address->sin_addr, &rule->remote_address, sizeof(in_addr)) != 0)
                {
                    continue;
                }
            }

            if ((rule->conditions & SOFTWALL_CONDITION_REMOTE_PORT) != 0)
            {
                if (remote_address == NULL ||
                    remote_address->sin_port != rule->remote_port)
                {
                    continue;
                }
            }

            *rule_retval = rule->retval;
            *rule_last_error = rule->last_error;

            return TRUE;
        }
    }
    __finally
    {
        ReleaseMutex(queue_mutex);
    }

    return FALSE;
}


/*
 * Utility functions
 */

DWORD
softwall_decide_from_addresses(const char *function_name,
                               DWORD return_address,
                               const sockaddr_in *local_address,
                               const sockaddr_in *remote_address,
                               BOOL *carry_on)
{
    BOOL found;
    int rule_retval;
    DWORD rule_last_error;

    found = softwall_find_rule(function_name, return_address,
                               local_address, remote_address,
                               &rule_retval, &rule_last_error);

    if (!found)
    {
        *carry_on = TRUE;
        return 0;
    }

    message_logger_log_message("Softwall", 0, MESSAGE_CTX_INFO,
        "found matching rule for %s called from 0x%08x",
        function_name, return_address);

    *carry_on = FALSE;

    SetLastError(rule_last_error);

    return rule_retval;
}

DWORD
softwall_decide_from_socket(const char *function_name,
                            DWORD return_address,
                            SOCKET s,
                            BOOL *carry_on)
{
	struct sockaddr_in local_address, remote_address;
	int len;

    /* get local and remote address */
    len = sizeof(local_address);
    getsockname(s, (struct sockaddr *) &local_address, &len);
    len = sizeof(remote_address);
    getpeername(s, (struct sockaddr *) &remote_address, &len);

    return softwall_decide_from_addresses(function_name, return_address,
                                          &local_address, &remote_address,
                                          carry_on);
}

DWORD
softwall_decide_from_socket_and_remote_address(const char *function_name,
                                               DWORD return_address,
                                               SOCKET s,
                                               const sockaddr_in *remote_address,
                                               BOOL *carry_on)
{
	struct sockaddr_in local_address;
	int len;

    /* get local address */
    len = sizeof(local_address);
    getsockname(s, (struct sockaddr *) &local_address, &len);

    return softwall_decide_from_addresses(function_name, return_address,
                                          &local_address, remote_address,
                                          carry_on);
}
