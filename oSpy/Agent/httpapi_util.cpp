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
#include "httpapi_util.h"
#include "logging.h"
#include "util.h"

static const char *HTTP_VERB_STRINGS[] =
{
    "Unparsed",
    "Unknown",
    "Invalid",
    "OPTIONS",
    "GET",
    "HEAD",
    "POST",
    "PUT",
    "DELETE",
    "TRACE",
    "CONNECT",
    "TRACK",
    "MOVE",
    "COPY",
    "PROPFIND",
    "PROPPATCH",
    "MKCOL",
    "LOCK",
    "UNLOCK",
    "SEARCH",
};

static const char *HTTP_HEADER_ID_STRINGS_REQUEST[] =
{
    "CacheControl",
    "Connection",
    "Date",
    "KeepAlive",
    "Pragma",
    "Trailer",
    "TransferEncoding",
    "Upgrade",
    "Via",
    "Warning",
    "Allow",
    "ContentLength",
    "ContentType",
    "ContentEncoding",
    "ContentLanguage",
    "ContentLocation",
    "ContentMd5",
    "ContentRange",
    "Expires",
    "LastModified",

    "Accept",
    "AcceptCharset",
    "AcceptEncoding",
    "AcceptLanguage",
    "Authorization",
    "Cookie",
    "Expect",
    "From",
    "Host",
    "IfMatch",
    "IfModifiedSince",
    "IfNoneMatch",
    "IfRange",
    "IfUnmodifiedSince",
    "MaxForwards",
    "ProxyAuthorization",
    "Referer",
    "Range",
    "Te",
    "Translate",
    "UserAgent",
};

static const char *HTTP_HEADER_ID_STRINGS_RESPONSE[] =
{
    "CacheControl",
    "Connection",
    "Date",
    "KeepAlive",
    "Pragma",
    "Trailer",
    "TransferEncoding",
    "Upgrade",
    "Via",
    "Warning",
    "Allow",
    "ContentLength",
    "ContentType",
    "ContentEncoding",
    "ContentLanguage",
    "ContentLocation",
    "ContentMd5",
    "ContentRange",
    "Expires",
    "LastModified",

    "AcceptRanges",
    "Age",
    "Etag",
    "Location",
    "ProxyAuthenticate",
    "RetryAfter",
    "Server",
    "SetCookie",
    "Vary",
    "WwwAuthenticate",
};

/* FIXME: this sucks.. better use an STL container here with a custom
 *        allocator that uses HeapAlloc/HeapFree ... */
#define MAX_TRACKED_REQUESTS 512

typedef struct {
    HTTP_REQUEST_ID req_id;
    SOCKADDR_IN local;
    SOCKADDR_IN remote;
} TrackedRequest;

static CRITICAL_SECTION lock;
static TrackedRequest requests[MAX_TRACKED_REQUESTS];

void
_httpapi_util_init()
{
    InitializeCriticalSection(&lock);
    memset(requests, 0, sizeof(requests));
}

static void
set_tracked_request(HTTP_REQUEST_ID req_id, SOCKADDR_IN *local, SOCKADDR_IN *remote)
{
    int i;

    EnterCriticalSection(&lock);

    for (i = 0; i < MAX_TRACKED_REQUESTS; i++)
    {
        TrackedRequest *req = &requests[i];

        if (req->req_id == 0 || req->req_id == req_id)
        {
            req->req_id = req_id;
            req->local = *local;
            req->remote = *remote;
            break;
        }
    }

    LeaveCriticalSection(&lock);
}

static void
get_tracked_request(HTTP_REQUEST_ID req_id, SOCKADDR_IN *local, SOCKADDR_IN *remote)
{
    int i;

    EnterCriticalSection(&lock);

    for (i = 0; i < MAX_TRACKED_REQUESTS; i++)
    {
        TrackedRequest *req = &requests[i];

        if (req->req_id == req_id)
        {
            *local = req->local;
            *remote = req->remote;
            break;
        }
    }

    LeaveCriticalSection(&lock);
}

static void
remove_tracked_request(HTTP_REQUEST_ID req_id)
{
    int i;

    EnterCriticalSection(&lock);

    for (i = 0; i < MAX_TRACKED_REQUESTS; i++)
    {
        TrackedRequest *req = &requests[i];

        if (req->req_id == req_id)
        {
            req->req_id = 0;
            break;
        }
    }

    LeaveCriticalSection(&lock);
}

static void
http_request_dump_headers(HTTP_REQUEST *req, ByteBuffer *buf)
{
    int i;

    for (i = 0; i < HttpHeaderRequestMaximum; i++)
    {
        HTTP_KNOWN_HEADER *hdr = &req->Headers.KnownHeaders[i];

        if (hdr->RawValueLength != 0)
        {
            byte_buffer_append_printf(buf, "%s: ", HTTP_HEADER_ID_STRINGS_REQUEST[i]);
            byte_buffer_append(buf, (void *) hdr->pRawValue, hdr->RawValueLength);
            byte_buffer_append_printf(buf, "\r\n");
        }
    }

    for (i = 0; i < req->Headers.UnknownHeaderCount; i++)
    {
        HTTP_UNKNOWN_HEADER *hdr = &req->Headers.pUnknownHeaders[i];

        byte_buffer_append(buf, (void *) hdr->pName, hdr->NameLength);
        byte_buffer_append_printf(buf, ": ");
        byte_buffer_append(buf, (void *) hdr->pRawValue, hdr->RawValueLength);
        byte_buffer_append_printf(buf, "\r\n");
    }

#if 0
    for (i = 0; i < req->EntityChunkCount; i++)
    {
        HTTP_DATA_CHUNK *chunk = &req->pEntityChunks[i];

        switch (chunk->DataChunkType) {
            case HttpDataChunkFromMemory:
                byte_buffer_append(buf, chunk->FromMemory.pBuffer,
                                   chunk->FromMemory.BufferLength);
                break;
            case HttpDataChunkFromFileHandle:
                message_logger_log_message("http_request_dump", 0,
                    MESSAGE_CTX_ERROR,
                    "DataChunkType == HttpDataChunkFromFileHandle not supported");
                break;
            case HttpDataChunkFromFragmentCache:
                message_logger_log_message("http_request_dump", 0,
                    MESSAGE_CTX_ERROR,
                    "DataChunkType == HttpDataChunkFromFragmentCache not supported");
                break;
        }
    }
#endif
}

static void
http_response_dump_headers(HTTP_RESPONSE *resp, ByteBuffer *buf)
{
    int i;

    for (i = 0; i < HttpHeaderResponseMaximum; i++)
    {
        HTTP_KNOWN_HEADER *hdr = &resp->Headers.KnownHeaders[i];

        if (hdr->RawValueLength != 0)
        {
            byte_buffer_append_printf(buf, "%s: ", HTTP_HEADER_ID_STRINGS_RESPONSE[i]);
            byte_buffer_append(buf, (void *) hdr->pRawValue, hdr->RawValueLength);
            byte_buffer_append_printf(buf, "\r\n");
        }
    }

    for (i = 0; i < resp->Headers.UnknownHeaderCount; i++)
    {
        HTTP_UNKNOWN_HEADER *hdr = &resp->Headers.pUnknownHeaders[i];

        byte_buffer_append(buf, (void *) hdr->pName, hdr->NameLength);
        byte_buffer_append_printf(buf, ": ");
        byte_buffer_append(buf, (void *) hdr->pRawValue, hdr->RawValueLength);
        byte_buffer_append_printf(buf, "\r\n");
    }
}

void
log_http_request(HANDLE queue_handle, HTTP_REQUEST *req,
                 const char *body, int body_size)
{
    ByteBuffer *buf = byte_buffer_sized_new(64);

    if (req)
    {
        if (req->Verb == HttpVerbUnknown)
        {
            byte_buffer_append(buf, (void *) req->pUnknownVerb, req->UnknownVerbLength);
        }
        else
        {
            byte_buffer_append_printf(buf, HTTP_VERB_STRINGS[req->Verb]);
        }

        byte_buffer_append_printf(buf, " ");
        byte_buffer_append(buf, (void *) req->pRawUrl, req->RawUrlLength);
        byte_buffer_append_printf(buf, " HTTP/%d.%d\r\n",
            req->Version.MajorVersion, req->Version.MinorVersion);

        http_request_dump_headers(req, buf);

        byte_buffer_append_printf(buf, "\r\n");
    }

    if (body != NULL && body_size > 0)
    {
        byte_buffer_append(buf, (void *) body, body_size);
    }

    message_logger_log_packet("HttpRequest",
        0, req->RequestId, PACKET_DIRECTION_INCOMING,
        (const sockaddr_in *) req->Address.pLocalAddress,
        (const sockaddr_in *) req->Address.pRemoteAddress,
        (const char *) buf->buf, (int) buf->offset);

    set_tracked_request(req->RequestId,
                        (SOCKADDR_IN *) req->Address.pLocalAddress,
                        (SOCKADDR_IN *) req->Address.pRemoteAddress);

    byte_buffer_free(buf);
}

void
log_http_response(HANDLE queue_handle,
                  HTTP_REQUEST_ID req_id,
                  HTTP_RESPONSE *resp,
                  const char *body,
                  int body_size)
{
    ByteBuffer *buf = byte_buffer_sized_new(64);
    SOCKADDR_IN local, remote;
    
    if (resp)
    {
        byte_buffer_append_printf(buf, "HTTP/%d.%d %d",
            resp->Version.MajorVersion, resp->Version.MinorVersion,
            resp->StatusCode);

        if (resp->ReasonLength > 0)
        {
            byte_buffer_append_printf(buf, " ");
            byte_buffer_append(buf, (void *) resp->pReason, resp->ReasonLength);
        }

        byte_buffer_append_printf(buf, "\r\n");

        http_response_dump_headers(resp, buf);

        byte_buffer_append_printf(buf, "\r\n");
    }

    if (body != NULL && body_size > 0)
    {
        byte_buffer_append(buf, (void *) body, body_size);
    }

    memset(&local, 0, sizeof(local));
    memset(&remote, 0, sizeof(remote));

    get_tracked_request(req_id, &local, &remote);

    if (body != NULL)
    {
        remove_tracked_request(req_id);
    }

    message_logger_log_packet("HttpResponse",
        0, req_id, PACKET_DIRECTION_OUTGOING, &local, &remote,
        (const char *) buf->buf, (int) buf->offset);

    byte_buffer_free(buf);
}
