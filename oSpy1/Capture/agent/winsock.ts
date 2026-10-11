import { captureBacktrace, reportLookup, reportMessage, reportPacket } from "./events";
import { Endpoint, softwallDecision } from "./softwall";
import { addrinfo } from "./addrinfo.hexpat";
import { OVERLAPPED_ENTRY } from "./overlapped_entry.hexpat";
import { sockaddr_in } from "./sockaddr.hexpat";
import { WSABUF } from "./wsabuf.hexpat";

const SOCKET_ERROR = -1;
const WSAEWOULDBLOCK = 10035;
const WSA_IO_PENDING = 997;
const WSAECONNRESET = 10054;
const INVALID_SOCKET = ptr(-1);
const AF_INET = 2;

interface BufferSlice {
  buf: NativePointer;
  len: number;
}

interface OverlappedOp {
  name: string;
  socket: NativePointer;
  direction: string;
  slices: BufferSlice[];
  backtrace: string;
}

export function instrumentWinsock(ws2: Module): void {
  const getsockname = new NativeFunction(ws2.getExportByName("getsockname"), "int", ["pointer", "pointer", "pointer"], "stdcall");
  const getpeername = new NativeFunction(ws2.getExportByName("getpeername"), "int", ["pointer", "pointer", "pointer"], "stdcall");
  const WSAGetLastError = new NativeFunction(ws2.getExportByName("WSAGetLastError"), "int", [], "stdcall");
  const connect = new SystemFunction(ws2.getExportByName("connect"), "int", ["pointer", "pointer", "int"], "stdcall");
  const overlappedOps = new Map<string, OverlappedOp>();

  Interceptor.replace(ws2.getExportByName("connect"), new NativeCallback(function (this: CallbackContext, socket: NativePointer, name: NativePointer, namelen: number): number {
    const verdict = softwallDecision({
      functionName: "connect",
      returnAddress: returnAddressRva(this.returnAddress),
      local: localRawEndpoint(socket),
      remote: rawEndpoint(name),
    });
    if (verdict !== null) {
      this.lastError = verdict.lastError;
      return verdict.retval;
    }

    const backtrace = captureBacktrace(this.context);
    const local = localEndpoint(socket);
    const peer = describeSockaddr(name);
    reportMessage("connect", resourceId(socket), "connecting", local, peer, backtrace, `${local}: connecting to ${peer}`);

    const result = connect(socket, name, namelen);
    const retval = result.value as number;
    if (retval !== SOCKET_ERROR)
      reportMessage("connect", resourceId(socket), "connected", local, peerEndpoint(socket), backtrace, `${local}: connected to ${peer}`);
    this.lastError = result.lastError as number;
    return retval;
  }, "int", ["pointer", "pointer", "int"], "stdcall"));

  Interceptor.attach(ws2.getExportByName("send"), {
    onEnter(args) {
      this.socket = args[0];
      this.buffer = args[1];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const sent = retval.toInt32();
      if (sent > 0)
        reportPacket("send", resourceId(this.socket), "outgoing", localEndpoint(this.socket), peerEndpoint(this.socket), this.backtrace, this.buffer, sent);
      else if (sent === SOCKET_ERROR)
        reportBroken("send", this.socket, this.backtrace, WSAGetLastError() as number);
    },
  });

  Interceptor.attach(ws2.getExportByName("recv"), {
    onEnter(args) {
      this.socket = args[0];
      this.buffer = args[1];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const received = retval.toInt32();
      if (received > 0)
        reportPacket("recv", resourceId(this.socket), "incoming", localEndpoint(this.socket), peerEndpoint(this.socket), this.backtrace, this.buffer, received);
      else if (received === 0)
        reportBroken("recv", this.socket, this.backtrace, null);
      else
        reportBroken("recv", this.socket, this.backtrace, WSAGetLastError() as number);
    },
  });

  Interceptor.attach(ws2.getExportByName("WSASend"), {
    onEnter(args) {
      this.socket = args[0];
      this.slices = readBufferSlices(args[1], args[2].toUInt32());
      this.bytesTransferred = args[3];
      this.overlapped = args[5];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      completeOrPend.call(this, "WSASend", "outgoing", retval);
    },
  });

  Interceptor.attach(ws2.getExportByName("WSARecv"), {
    onEnter(args) {
      this.socket = args[0];
      this.slices = readBufferSlices(args[1], args[2].toUInt32());
      this.bytesTransferred = args[3];
      this.overlapped = args[5];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      completeOrPend.call(this, "WSARecv", "incoming", retval);
    },
  });

  Interceptor.attach(Process.getModuleByName("kernel32.dll").getExportByName("GetQueuedCompletionStatus"), {
    onEnter(args) {
      this.bytes = args[1];
      this.overlapped = args[3];
    },
    onLeave(retval) {
      if (retval.toInt32() !== 0)
        completeOverlapped(this.overlapped.readPointer(), this.bytes.readU32());
    },
  });

  Interceptor.attach(Process.getModuleByName("kernel32.dll").getExportByName("GetQueuedCompletionStatusEx"), {
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
        completeOverlapped(entry.lpOverlapped, entry.dwNumberOfBytesTransferred);
      }
    },
  });

  Interceptor.attach(ws2.getExportByName("WSAGetOverlappedResult"), {
    onEnter(args) {
      this.overlapped = args[1];
      this.transfer = args[2];
    },
    onLeave(retval) {
      if (retval.toInt32() !== 0)
        completeOverlapped(this.overlapped, this.transfer.readU32());
    },
  });

  Interceptor.attach(ws2.getExportByName("sendto"), {
    onEnter(args) {
      this.socket = args[0];
      this.buffer = args[1];
      this.peer = describeSockaddr(args[4]);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const sent = retval.toInt32();
      if (sent > 0)
        reportPacket("sendto", resourceId(this.socket), "outgoing", localEndpoint(this.socket), this.peer, this.backtrace, this.buffer, sent);
    },
  });

  Interceptor.attach(ws2.getExportByName("recvfrom"), {
    onEnter(args) {
      this.socket = args[0];
      this.buffer = args[1];
      this.from = args[4];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const received = retval.toInt32();
      if (received > 0)
        reportPacket("recvfrom", resourceId(this.socket), "incoming", localEndpoint(this.socket), describeSockaddr(this.from), this.backtrace, this.buffer, received);
    },
  });

  Interceptor.attach(ws2.getExportByName("accept"), {
    onEnter(args) {
      instrumentAccept.call(this, "accept", args[0]);
    },
    onLeave(retval) {
      completeAccept.call(this, "accept", retval);
    },
  });

  Interceptor.attach(ws2.getExportByName("WSAAccept"), {
    onEnter(args) {
      instrumentAccept.call(this, "WSAAccept", args[0]);
    },
    onLeave(retval) {
      completeAccept.call(this, "WSAAccept", retval);
    },
  });

  Interceptor.attach(ws2.getExportByName("getaddrinfo"), {
    onEnter(args) {
      this.node = readAnsiString(args[0]);
      this.service = readAnsiString(args[1]);
      this.hints = args[2];
      this.result = args[3];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const lines = [
        `nodename: ${this.node}`,
        `servname: ${this.service}`,
        `hints:${describeAddrinfo(this.hints)}`,
      ];
      if (retval.toInt32() === 0)
        lines.push(`result:${describeAddrinfo(this.result.readPointer())}`);
      else
        lines.push(`result:\r\n  error 0x${(retval.toInt32() >>> 0).toString(16)}`);
      reportLookup("getaddrinfo", this.backtrace, lines.join("\r\n"));
    },
  });

  Interceptor.attach(ws2.getExportByName("closesocket"), {
    onEnter(args) {
      this.socket = args[0];
      const local = localEndpoint(args[0]);
      reportMessage("closesocket", resourceId(args[0]), "disconnected", local, peerEndpoint(args[0]), captureBacktrace(this.context),
        local !== "" ? `${local}: closed` : "");
    },
    onLeave() {
      releaseResourceId(this.socket);
    },
  });

  function instrumentAccept(this: any, name: string, server: NativePointer): void {
    this.server = server;
    this.serverLocal = localEndpoint(server);
    this.backtrace = captureBacktrace(this.context);
    reportMessage(name, resourceId(server), "listening", this.serverLocal, "", this.backtrace,
      `${this.serverLocal}: listening for connections`);
  }

  function completeAccept(this: any, name: string, client: NativePointer): void {
    if (client.equals(INVALID_SOCKET))
      return;

    const clientPeer = peerEndpoint(client);
    reportMessage(name, resourceId(client), "connected", this.serverLocal, clientPeer, this.backtrace,
      `${this.serverLocal}: client connected from ${clientPeer}`);
  }

  function completeOrPend(this: any, name: string, direction: string, retval: NativePointer): void {
    if (retval.toInt32() === 0) {
      reportSlices(name, this.socket, direction, this.slices, this.bytesTransferred.readU32(), this.backtrace);
      return;
    }

    if (retval.toInt32() !== SOCKET_ERROR)
      return;

    const error = WSAGetLastError() as number;
    if (error === WSA_IO_PENDING && !this.overlapped.isNull()) {
      overlappedOps.set(this.overlapped.toString(),
        { name, socket: this.socket, direction, slices: this.slices, backtrace: this.backtrace });
      return;
    }

    reportWsaBroken(name, this.socket, this.backtrace, error);
  }

  function completeOverlapped(overlapped: NativePointer, transferred: number): void {
    if (overlapped.isNull())
      return;

    const key = overlapped.toString();
    const op = overlappedOps.get(key);
    if (op === undefined)
      return;

    overlappedOps.delete(key);
    if (transferred > 0)
      reportSlices(op.name, op.socket, op.direction, op.slices, transferred, op.backtrace);
  }

  function readBufferSlices(buffers: NativePointer, count: number): BufferSlice[] {
    const slices: BufferSlice[] = [];
    for (let i = 0; i !== count; i++) {
      const buffer = WSABUF.at(buffers.add(i * WSABUF.size));
      slices.push({ buf: buffer.buf, len: buffer.len });
    }
    return slices;
  }

  function reportSlices(name: string, socket: NativePointer, direction: string, slices: BufferSlice[], transferred: number, backtrace: string): void {
    const local = localEndpoint(socket);
    const peer = peerEndpoint(socket);

    let left = transferred;
    for (const slice of slices) {
      if (left <= 0)
        break;
      const n = Math.min(slice.len, left);
      reportPacket(name, resourceId(socket), direction, local, peer, backtrace, slice.buf, n);
      left -= n;
    }
  }

  function reportWsaBroken(name: string, socket: NativePointer, backtrace: string, wsaError: number): void {
    if (wsaError === WSAEWOULDBLOCK) {
      reportMessage(name, resourceId(socket), "warning", "", "", backtrace, "non-blocking mode not yet supported");
      return;
    }

    if (wsaError === WSA_IO_PENDING)
      return;

    reportBroken(name, socket, backtrace, (wsaError === WSAECONNRESET) ? null : wsaError);
  }

  function reportBroken(name: string, socket: NativePointer, backtrace: string, error: number | null): void {
    if (error === WSAEWOULDBLOCK)
      return;

    const local = localEndpoint(socket);
    const peer = peerEndpoint(socket);
    const context = (error === null) ? "disconnected" : "reset";
    const verb = (error === null) ? "closed" : "reset";
    reportMessage(name, resourceId(socket), context, local, peer, backtrace, `${local}: connection to ${peer} ${verb}`);
  }

  function localEndpoint(socket: NativePointer): string {
    return resolveEndpoint(socket, getsockname);
  }

  function peerEndpoint(socket: NativePointer): string {
    return resolveEndpoint(socket, getpeername);
  }

  function resolveEndpoint(socket: NativePointer, resolve: NativeFunction<number, [NativePointer, NativePointer, NativePointer]>): string {
    const storage = Memory.alloc(16);
    const length = Memory.alloc(4);
    length.writeInt(16);

    if (resolve(socket, storage, length) !== 0)
      return "";

    return describeSockaddr(storage);
  }

  function localRawEndpoint(socket: NativePointer): Endpoint | null {
    const storage = Memory.alloc(16);
    const length = Memory.alloc(4);
    length.writeInt(16);

    if (getsockname(socket, storage, length) !== 0)
      return null;

    return rawEndpoint(storage);
  }
}

function rawEndpoint(storage: NativePointer): Endpoint | null {
  if (storage.isNull() || storage.readU16() !== AF_INET)
    return null;

  return {
    address: storage.add(4).readU32() >>> 0,
    port: storage.add(2).readU16(),
  };
}

function returnAddressRva(address: NativePointer): number {
  const module = Process.findModuleByAddress(address);
  if (module === null)
    return 0;

  return address.sub(module.base).toUInt32();
}

function describeSockaddr(storage: NativePointer): string {
  if (storage.isNull())
    return "";

  const address = sockaddr_in.at(storage);
  if (address.sin_family !== AF_INET)
    return "";

  const host = [address.sin_addr[0], address.sin_addr[1], address.sin_addr[2], address.sin_addr[3]].join(".");
  return host + ":" + address.sin_port;
}

function describeAddrinfo(head: NativePointer): string {
  if (head.isNull())
    return " none";

  let text = "";
  for (let node: any = addrinfo.at(head); node !== null; node = node.ai_next) {
    text += `\r\n  family=${node.ai_family} socktype=${node.ai_socktype} protocol=${node.ai_protocol} flags=0x${(node.ai_flags >>> 0).toString(16)}`;
    if (!node.ai_canonname.isNull())
      text += ` canonname=${readAnsiString(node.ai_canonname)}`;
    const endpoint = describeSockaddr(node.ai_addr);
    if (endpoint !== "")
      text += ` addr=${endpoint}`;
  }
  return text;
}

function readAnsiString(pointer: NativePointer): string {
  return pointer.isNull() ? "NULL" : pointer.readAnsiString()!;
}

const socketIds = new Map<string, number>();

function resourceId(socket: NativePointer): number {
  const key = socket.toString();
  let id = socketIds.get(key);
  if (id === undefined) {
    id = (Math.random() * 0xffffffff) >>> 0;
    socketIds.set(key, id);
  }
  return id;
}

function releaseResourceId(socket: NativePointer): void {
  socketIds.delete(socket.toString());
}
