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
#include "byte_buffer.h"
#include "logging_old.h"

// FIXME: this code must die (switching to C++)

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
