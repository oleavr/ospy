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

class CHookContext;

extern CHookContext g_getaddrinfoHookContext;
extern CHookContext g_recvHookContext;
extern CHookContext g_sendHookContext;
extern CHookContext g_connectHookContext;
extern CHookContext g_encryptMessageHookContext;
extern CHookContext g_decryptMessageHookContext;

void hook_winsock();
void hook_secur32();
void hook_crypt();
void hook_wininet();
void hook_httpapi();
void hook_activesync();
void hook_msn();