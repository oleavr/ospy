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
#include "util.h"

#pragma managed(push, off)

static SoftwallRule *g_rules;
static unsigned int g_num_rules;

void
softwall_init(const SoftwallRule *rules, unsigned int num_rules)
{
    unsigned int size = num_rules * sizeof(SoftwallRule);
    g_rules = (SoftwallRule *) malloc(size);
    memcpy(g_rules, rules, size);
    g_num_rules = num_rules;
}

static BOOL
softwall_find_rule(const char *function_name,
                   DWORD return_address,
                   const sockaddr_in *local_address,
                   const sockaddr_in *remote_address,
                   int *rule_retval,
                   DWORD *rule_last_error)
{
    for (unsigned int i = 0; i < g_num_rules; i++)
    {
        SoftwallRule *rule = &g_rules[i];

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

#pragma managed(pop)
