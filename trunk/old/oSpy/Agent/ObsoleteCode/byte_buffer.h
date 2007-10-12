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

// FIXME: this code must die (switching to C++)

typedef struct {
    void *buf;
    size_t size;
    size_t offset;
} ByteBuffer;

ByteBuffer *byte_buffer_sized_new(size_t size);
void byte_buffer_free(ByteBuffer *buf);

void byte_buffer_append(ByteBuffer *buf, void *bytes, size_t n);
void byte_buffer_append_printf(ByteBuffer *buf, const char *fmt, ...);
