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
#include "byte_buffer.h"
#include "logging.h"

#pragma managed(push, off)

ByteBuffer *
byte_buffer_sized_new(size_t size)
{
    ByteBuffer *buf = (ByteBuffer *) sspy_malloc(sizeof(ByteBuffer));

    buf->buf = sspy_malloc(size);
    buf->size = size;
    buf->offset = 0;

    return buf;
}

void
byte_buffer_free(ByteBuffer *buf)
{
    sspy_free(buf->buf);
    sspy_free(buf);
}

void
byte_buffer_append(ByteBuffer *buf, void *bytes, size_t n)
{
    if (n < 1)
        return;

    size_t bytes_left = buf->size - buf->offset;
    if (bytes_left < n)
    {
        size_t new_size = (buf->size * 2) + n;

        void *p = sspy_realloc(buf->buf, new_size);
        if (p == NULL)
        {
            /* shouldn't happen */
            message_logger_log_message("raw_buffer_append", 0,
                MESSAGE_CTX_ERROR, "realloc failed");
            return;
        }

        buf->buf = p;
        buf->size = new_size;
    }

    memcpy((char *) buf->buf + buf->offset, bytes, n);
    buf->offset += n;
}

void
byte_buffer_append_printf(ByteBuffer *buf, const char *fmt, ...)
{
    va_list args;
    char tmp[4096];

    va_start(args, fmt);
    vsprintf(tmp, fmt, args);

    byte_buffer_append(buf, tmp, strlen(tmp));
}

#pragma managed(pop)
