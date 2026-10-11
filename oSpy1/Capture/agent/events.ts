const MAX_PAYLOAD = 65536;
const MAX_BATCH = 128;
const FLUSH_INTERVAL = 25;

interface Event {
  kind: string;
  name: string;
  threadId: number;
  resourceId: number;
  direction?: string;
  context?: string;
  localAddress?: string;
  peerAddress?: string;
  backtrace: string;
  text?: string;
  dataSize: number;
}

const pendingEvents: Event[] = [];
const pendingData: ArrayBuffer[] = [];
let flushTimer: any = null;

export function reportPacket(name: string, resourceId: number, direction: string, localAddress: string,
    peerAddress: string, backtrace: string, buffer: NativePointer, length: number): void {
  const data = readPayload(buffer, length);
  enqueue({
    kind: "packet",
    name,
    threadId: Process.getCurrentThreadId(),
    resourceId,
    direction,
    localAddress,
    peerAddress,
    backtrace,
    dataSize: data.byteLength,
  }, data);
}

export function reportPacketBytes(name: string, resourceId: number, direction: string, localAddress: string,
    peerAddress: string, backtrace: string, data: ArrayBuffer): void {
  enqueue({
    kind: "packet",
    name,
    threadId: Process.getCurrentThreadId(),
    resourceId,
    direction,
    localAddress,
    peerAddress,
    backtrace,
    dataSize: data.byteLength,
  }, data);
}

export function reportMessage(name: string, resourceId: number, context: string, localAddress: string,
    peerAddress: string, backtrace: string, text: string): void {
  enqueue({
    kind: "message",
    name,
    threadId: Process.getCurrentThreadId(),
    resourceId,
    context,
    localAddress,
    peerAddress,
    backtrace,
    text,
    dataSize: 0,
  }, null);
}

export function reportLookup(name: string, backtrace: string, text: string): void {
  const data = encodeUtf8(text);
  enqueue({
    kind: "packet",
    name,
    threadId: Process.getCurrentThreadId(),
    resourceId: 0,
    direction: "invalid",
    localAddress: "",
    peerAddress: "",
    backtrace,
    dataSize: data.byteLength,
  }, data);
}

export function flush(): void {
  if (flushTimer !== null) {
    clearTimeout(flushTimer);
    flushTimer = null;
  }

  if (pendingEvents.length === 0)
    return;

  const events = pendingEvents.splice(0);
  const data = concat(pendingData);
  pendingData.length = 0;

  if (data.byteLength !== 0)
    send({ type: "events", events }, data);
  else
    send({ type: "events", events });
}

export function captureBacktrace(context: CpuContext): string {
  return Thread.backtrace(context, Backtracer.FUZZY)
    .map(describeReturnAddress)
    .filter(frame => frame !== null)
    .slice(0, 8)
    .join("\n");
}

function enqueue(event: Event, data: ArrayBuffer | null): void {
  pendingEvents.push(event);
  if (data !== null)
    pendingData.push(data);

  if (pendingEvents.length >= MAX_BATCH)
    flush();
  else if (flushTimer === null)
    flushTimer = setTimeout(flush, FLUSH_INTERVAL);
}

function concat(buffers: ArrayBuffer[]): ArrayBuffer {
  let total = 0;
  for (const buffer of buffers)
    total += buffer.byteLength;

  const combined = new Uint8Array(total);
  let offset = 0;
  for (const buffer of buffers) {
    combined.set(new Uint8Array(buffer), offset);
    offset += buffer.byteLength;
  }
  return combined.buffer;
}

function readPayload(buffer: NativePointer, length: number): ArrayBuffer {
  if (buffer.isNull() || length <= 0)
    return new ArrayBuffer(0);

  return buffer.readByteArray(Math.min(length, MAX_PAYLOAD))!;
}

function encodeUtf8(text: string): ArrayBuffer {
  const bytes = new Uint8Array(text.length);
  for (let i = 0; i !== text.length; i++)
    bytes[i] = text.charCodeAt(i) & 0xff;
  return bytes.buffer;
}

function describeReturnAddress(address: NativePointer): string | null {
  const module = Process.findModuleByAddress(address);
  if (module === null)
    return null;

  const offset = address.sub(module.base).toUInt32();
  return module.name + "::0x" + offset.toString(16);
}
