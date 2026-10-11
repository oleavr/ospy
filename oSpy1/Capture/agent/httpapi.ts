import { captureBacktrace, reportPacket, reportPacketBytes } from "./events";
import { HTTP_DATA_CHUNK, HTTP_KNOWN_HEADER, HTTP_REQUEST, HTTP_RESPONSE, HTTP_UNKNOWN_HEADER, HttpDataChunkType, HttpVerb } from "./http.hexpat";
import { OVERLAPPED_ENTRY } from "./overlapped_entry.hexpat";
import { sockaddr_in } from "./sockaddr.hexpat";

const NO_ERROR = 0;
const ERROR_IO_PENDING = 997;
const AF_INET = 2;
const WIDE = (Process.pointerSize === 4) ? 1 : 0;

export function instrumentHttpapi(httpapi: Module): void {
  Interceptor.attach(httpapi.getExportByName("HttpReceiveHttpRequest"), {
    onEnter(args) {
      this.buffer = args[3 + WIDE];
      this.overlapped = args[6 + WIDE];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const result = retval.toInt32();
      if (result === NO_ERROR)
        reportRequest(this.buffer, this.backtrace);
      else if (result === ERROR_IO_PENDING && !this.overlapped.isNull())
        pendingReceives.set(this.overlapped.toString(), { kind: "request", buffer: this.buffer, backtrace: this.backtrace });
    },
  });

  Interceptor.attach(httpapi.getExportByName("HttpReceiveRequestEntityBody"), {
    onEnter(args) {
      this.requestKey = argRequestKey(args);
      this.resourceId = args[1].toUInt32() >>> 0;
      this.buffer = args[3 + WIDE];
      this.bytesReceived = args[5 + WIDE];
      this.overlapped = args[6 + WIDE];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const result = retval.toInt32();
      if (result === NO_ERROR)
        reportRequestBody(this.requestKey, this.resourceId, this.buffer, this.bytesReceived.readU32(), this.backtrace);
      else if (result === ERROR_IO_PENDING && !this.overlapped.isNull())
        pendingReceives.set(this.overlapped.toString(),
          { kind: "body", buffer: this.buffer, requestKey: this.requestKey, resourceId: this.resourceId, backtrace: this.backtrace });
    },
  });

  Interceptor.attach(httpapi.getExportByName("HttpSendHttpResponse"), {
    onEnter(args) {
      this.requestKey = argRequestKey(args);
      this.resourceId = args[1].toUInt32() >>> 0;
      this.response = args[3 + WIDE];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === NO_ERROR)
        reportResponse(this.response, this.requestKey, this.resourceId, this.backtrace);
    },
  });

  Interceptor.attach(httpapi.getExportByName("HttpSendResponseEntityBody"), {
    onEnter(args) {
      this.requestKey = argRequestKey(args);
      this.resourceId = args[1].toUInt32() >>> 0;
      this.chunkCount = args[3 + WIDE].toUInt32();
      this.chunks = args[4 + WIDE];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() !== NO_ERROR || this.chunkCount === 0)
        return;
      const chunk = HTTP_DATA_CHUNK.at(this.chunks);
      if (chunk.DataChunkType === HttpDataChunkType.FromMemory)
        reportResponseBody(this.requestKey, this.resourceId, chunk.pBuffer, chunk.BufferLength, this.backtrace);
    },
  });

  const kernel32 = Process.getModuleByName("kernel32.dll");

  Interceptor.attach(kernel32.getExportByName("GetOverlappedResult"), {
    onEnter(args) {
      this.overlapped = args[1];
      this.bytes = args[2];
    },
    onLeave(retval) {
      if (retval.toInt32() !== 0)
        completeReceive(this.overlapped, this.bytes.readU32());
    },
  });

  Interceptor.attach(kernel32.getExportByName("GetQueuedCompletionStatus"), {
    onEnter(args) {
      this.bytes = args[1];
      this.overlapped = args[3];
    },
    onLeave(retval) {
      if (retval.toInt32() !== 0)
        completeReceive(this.overlapped.readPointer(), this.bytes.readU32());
    },
  });

  Interceptor.attach(kernel32.getExportByName("GetQueuedCompletionStatusEx"), {
    onEnter(args) {
      this.entries = args[1];
      this.removed = args[3];
    },
    onLeave(retval) {
      if (retval.toInt32() === 0)
        return;
      const removed = this.removed.readU32();
      for (let i = 0; i !== removed; i++) {
        const entry = OVERLAPPED_ENTRY.at(this.entries.add(i * OVERLAPPED_ENTRY.size));
        completeReceive(entry.lpOverlapped, entry.dwNumberOfBytesTransferred);
      }
    },
  });
}

function reportRequest(buffer: NativePointer, backtrace: string): void {
  const request = HTTP_REQUEST.parse(buffer);
  const key = requestKey(request.RequestId);
  const local = describeEndpoint(request.Address.pLocalAddress);
  const peer = describeEndpoint(request.Address.pRemoteAddress);
  trackedRequests.set(key, { local, peer });

  const parts: Uint8Array[] = [];
  appendVerb(parts, request);
  appendText(parts, " ");
  appendRaw(parts, request.pRawUrl, request.RawUrlLength);
  appendText(parts, ` HTTP/${request.Version.MajorVersion}.${request.Version.MinorVersion}\r\n`);
  appendKnownHeaders(parts, request.Headers.KnownHeaders, REQUEST_HEADER_NAMES);
  appendUnknownHeaders(parts, request.Headers.pUnknownHeaders, request.Headers.UnknownHeaderCount);
  appendText(parts, "\r\n");

  reportPacketBytes("HttpRequest", lowWord(request.RequestId), "incoming", local, peer, backtrace, concatParts(parts));
}

function reportResponse(buffer: NativePointer, key: string, resourceId: number, backtrace: string): void {
  const response = HTTP_RESPONSE.parse(buffer);
  const addresses = trackedRequests.get(key);

  const parts: Uint8Array[] = [];
  appendText(parts, `HTTP/${response.Version.MajorVersion}.${response.Version.MinorVersion} ${response.StatusCode}`);
  if (response.ReasonLength > 0) {
    appendText(parts, " ");
    appendRaw(parts, response.pReason, response.ReasonLength);
  }
  appendText(parts, "\r\n");
  appendKnownHeaders(parts, response.Headers.KnownHeaders, RESPONSE_HEADER_NAMES);
  appendUnknownHeaders(parts, response.Headers.pUnknownHeaders, response.Headers.UnknownHeaderCount);
  appendText(parts, "\r\n");

  reportPacketBytes("HttpResponse", resourceId, "outgoing", addresses?.local ?? "", addresses?.peer ?? "", backtrace, concatParts(parts));
}

function reportRequestBody(key: string, resourceId: number, buffer: NativePointer, length: number, backtrace: string): void {
  if (buffer.isNull() || length === 0)
    return;
  const addresses = trackedRequests.get(key);
  reportPacket("HttpRequest", resourceId, "incoming", addresses?.local ?? "", addresses?.peer ?? "", backtrace, buffer, length);
}

function reportResponseBody(key: string, resourceId: number, buffer: NativePointer, length: number, backtrace: string): void {
  const addresses = trackedRequests.get(key);
  if (!buffer.isNull() && length !== 0)
    reportPacket("HttpResponse", resourceId, "outgoing", addresses?.local ?? "", addresses?.peer ?? "", backtrace, buffer, length);
  trackedRequests.delete(key);
}

function completeReceive(overlapped: NativePointer, bytes: number): void {
  if (overlapped.isNull())
    return;

  const key = overlapped.toString();
  const op = pendingReceives.get(key);
  if (op === undefined)
    return;

  pendingReceives.delete(key);
  if (op.kind === "request")
    reportRequest(op.buffer, op.backtrace);
  else
    reportRequestBody(op.requestKey!, op.resourceId!, op.buffer, bytes, op.backtrace);
}

function appendVerb(parts: Uint8Array[], request: HTTP_REQUEST.Parsed): void {
  if (request.Verb === HttpVerb.Unknown)
    appendRaw(parts, request.pUnknownVerb, request.UnknownVerbLength);
  else
    appendText(parts, HttpVerb[request.Verb] ?? "");
}

function appendKnownHeaders(parts: Uint8Array[], headers: HTTP_KNOWN_HEADER.Parsed[], names: string[]): void {
  for (let i = 0; i !== names.length; i++) {
    const header = headers[i];
    if (header.RawValueLength === 0)
      continue;
    appendText(parts, names[i] + ": ");
    appendRaw(parts, header.pRawValue, header.RawValueLength);
    appendText(parts, "\r\n");
  }
}

function appendUnknownHeaders(parts: Uint8Array[], pointer: NativePointer, count: number): void {
  for (let i = 0; i !== count; i++) {
    const header = HTTP_UNKNOWN_HEADER.at(pointer.add(i * HTTP_UNKNOWN_HEADER.size));
    appendRaw(parts, header.pName, header.NameLength);
    appendText(parts, ": ");
    appendRaw(parts, header.pRawValue, header.RawValueLength);
    appendText(parts, "\r\n");
  }
}

function appendText(parts: Uint8Array[], text: string): void {
  const bytes = new Uint8Array(text.length);
  for (let i = 0; i !== text.length; i++)
    bytes[i] = text.charCodeAt(i) & 0xff;
  parts.push(bytes);
}

function appendRaw(parts: Uint8Array[], pointer: NativePointer, length: number): void {
  if (pointer.isNull() || length === 0)
    return;
  parts.push(new Uint8Array(pointer.readByteArray(length)!));
}

function concatParts(parts: Uint8Array[]): ArrayBuffer {
  let total = 0;
  for (const part of parts)
    total += part.length;

  const combined = new Uint8Array(total);
  let offset = 0;
  for (const part of parts) {
    combined.set(part, offset);
    offset += part.length;
  }
  return combined.buffer;
}

function describeEndpoint(address: NativePointer): string {
  if (address.isNull() || address.readU16() !== AF_INET)
    return "";

  const sa = sockaddr_in.at(address);
  const host = [sa.sin_addr[0], sa.sin_addr[1], sa.sin_addr[2], sa.sin_addr[3]].join(".");
  return host + ":" + sa.sin_port;
}

function argRequestKey(args: InvocationArguments): string {
  const low = args[1].toUInt32() >>> 0;
  const high = ((WIDE === 1) ? args[2].toUInt32() : args[1].shr(32).toUInt32()) >>> 0;
  return highLow(high, low);
}

function requestKey(value: UInt64): string {
  return highLow(value.shr(32).toNumber() >>> 0, value.and(0xffffffff).toNumber() >>> 0);
}

function highLow(high: number, low: number): string {
  return high.toString(16).padStart(8, "0") + low.toString(16).padStart(8, "0");
}

function lowWord(value: UInt64): number {
  return value.and(0xffffffff).toNumber() >>> 0;
}

interface PendingReceive {
  kind: "request" | "body";
  buffer: NativePointer;
  backtrace: string;
  requestKey?: string;
  resourceId?: number;
}

interface TrackedRequest {
  local: string;
  peer: string;
}

const pendingReceives = new Map<string, PendingReceive>();
const trackedRequests = new Map<string, TrackedRequest>();

const GENERAL_HEADER_NAMES = [
  "Cache-Control", "Connection", "Date", "Keep-Alive", "Pragma", "Trailer", "Transfer-Encoding", "Upgrade", "Via", "Warning",
  "Allow", "Content-Length", "Content-Type", "Content-Encoding", "Content-Language", "Content-Location", "Content-MD5",
  "Content-Range", "Expires", "Last-Modified",
];

const REQUEST_HEADER_NAMES = [
  ...GENERAL_HEADER_NAMES,
  "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Authorization", "Cookie", "Expect", "From", "Host",
  "If-Match", "If-Modified-Since", "If-None-Match", "If-Range", "If-Unmodified-Since", "Max-Forwards", "Proxy-Authorization",
  "Referer", "Range", "TE", "Translate", "User-Agent",
];

const RESPONSE_HEADER_NAMES = [
  ...GENERAL_HEADER_NAMES,
  "Accept-Ranges", "Age", "ETag", "Location", "Proxy-Authenticate", "Retry-After", "Server", "Set-Cookie", "Vary",
  "WWW-Authenticate",
];
