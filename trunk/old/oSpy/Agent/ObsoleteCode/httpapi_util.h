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

#include <http.h>

void _httpapi_util_init();

void log_http_request(HANDLE queue_handle, HTTP_REQUEST *req, const char *body, int body_size);
void log_http_response(HANDLE queue_handle, HTTP_REQUEST_ID req_id, HTTP_RESPONSE *resp, const char *body, int body_size);
