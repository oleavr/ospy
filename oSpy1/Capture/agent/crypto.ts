import { captureBacktrace, reportMessage, reportPacket } from "./events";
import { AlgId, HashParam, HMAC_INFO, KeyParam } from "./crypt.hexpat";
import { SecBuffer, SecBufferDesc } from "./secbuffer.hexpat";

const SECBUFFER_DATA = 1;

export function instrumentSecur32(secur32: Module): void {
  Interceptor.attach(secur32.getExportByName("EncryptMessage"), {
    onEnter(args) {
      if (calledInternally(this.returnAddress, SECUR32_INTERNAL_MODULES))
        return;
      reportSecurityBuffers("EncryptMessage", args[0], args[2], "outgoing", captureBacktrace(this.context));
    },
  });

  Interceptor.attach(secur32.getExportByName("DecryptMessage"), {
    onEnter(args) {
      this.securityContext = args[0];
      this.message = args[1];
      this.internal = calledInternally(this.returnAddress, SECUR32_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave() {
      if (this.internal)
        return;
      reportSecurityBuffers("DecryptMessage", this.securityContext, this.message, "incoming", this.backtrace);
    },
  });

  Interceptor.attach(secur32.getExportByName("DeleteSecurityContext"), {
    onEnter(args) {
      releaseContextId(args[0]);
    },
  });
}

export function instrumentCrypt(advapi32: Module): void {
  const cryptGetKeyParam = new NativeFunction(advapi32.getExportByName("CryptGetKeyParam"), "int",
    ["pointer", "uint", "pointer", "pointer", "uint"], "stdcall");
  let queryingBlockLength = false;

  instrumentCipher("CryptEncrypt");
  instrumentCipher("CryptDecrypt");

  Interceptor.attach(advapi32.getExportByName("CryptImportKey"), {
    onEnter(args) {
      this.blob = args[1];
      this.blobSize = args[2].toUInt32();
      this.outKey = args[5];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      emitPacket("CryptImportKey", handleId(this.outKey.readPointer()), this.backtrace, this.blob, this.blobSize);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptExportKey"), {
    onEnter(args) {
      this.key = args[0];
      this.blob = args[4];
      this.blobSize = args[5];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      emitPacket("CryptExportKey", handleId(this.key), this.backtrace, this.blob, this.blobSize.readU32());
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptGenKey"), {
    onEnter(args) {
      this.prov = args[0];
      this.algId = args[1].toUInt32();
      this.flags = args[2].toUInt32();
      this.outKey = args[3];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      const key = this.outKey.readPointer();
      reportMessage("CryptGenKey", handleId(key), "info", "", "", this.backtrace,
        `hProv=0x${this.prov.toString(16)} Algid=${algName(this.algId)} dwFlags=0x${hex32(this.flags)} => *phKey=0x${key.toString(16)}`);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptDuplicateKey"), {
    onEnter(args) {
      this.key = args[0];
      this.outKey = args[3];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      const key = this.outKey.readPointer();
      reportMessage("CryptDuplicateKey", handleId(key), "info", "", "", this.backtrace,
        `hKey=0x${this.key.toString(16)} => *phKey=0x${key.toString(16)}`);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptGetKeyParam"), {
    onEnter(args) {
      this.key = args[0];
      this.data = args[2];
      this.dataLen = args[3];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal || queryingBlockLength)
        return;
      emitPacket("CryptGetKeyParam", handleId(this.key), this.backtrace, this.data, this.dataLen.readU32());
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptSetKeyParam"), {
    onEnter(args) {
      this.key = args[0];
      this.param = args[1].toUInt32();
      this.data = args[2];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      emitPacket("CryptSetKeyParam", handleId(this.key), this.backtrace, this.data, keyParamLength(this.param, this.key));
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptDestroyKey"), {
    onEnter(args) {
      this.key = args[0];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      reportMessage("CryptDestroyKey", handleId(this.key), "info", "", "", this.backtrace, `hKey=0x${this.key.toString(16)}`);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptGenRandom"), {
    onEnter(args) {
      this.prov = args[0];
      this.length = args[1].toUInt32();
      this.buffer = args[2];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      emitPacket("CryptGenRandom", handleId(this.prov), this.backtrace, this.buffer, this.length);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptCreateHash"), {
    onEnter(args) {
      this.prov = args[0];
      this.algId = args[1].toUInt32();
      this.key = args[2];
      this.outHash = args[4];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0)
        return;
      const hash = this.outHash.readPointer();
      const context = trackHash(hash, this.algId);
      if (this.internal)
        return;
      reportMessage("CryptCreateHash", context.id, "info", "", "", this.backtrace,
        `hProv=0x${this.prov.toString(16)} Algid=${algName(this.algId)} hKey=0x${this.key.toString(16)} => *phHash=0x${hash.toString(16)}`);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptDuplicateHash"), {
    onEnter(args) {
      this.hash = args[0];
      this.outHash = args[3];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      const context = findHash(this.hash);
      if (context === null)
        return;
      reportMessage("CryptDuplicateHash", context.id, "info", "", "", this.backtrace,
        `hHash=0x${this.hash.toString(16)} Algid=${algName(context.algId)} => *phHash=0x${this.outHash.readPointer().toString(16)}`);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptDestroyHash"), {
    onEnter(args) {
      this.hash = args[0];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0)
        return;
      const context = findHash(this.hash);
      if (context === null)
        return;
      if (!this.internal)
        reportMessage("CryptDestroyHash", context.id, "info", "", "", this.backtrace, `hHash=0x${this.hash.toString(16)}`);
      releaseHash(this.hash);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptHashData"), {
    onEnter(args) {
      this.hash = args[0];
      this.data = args[1];
      this.length = args[2].toUInt32();
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      const context = findHash(this.hash);
      if (context === null)
        return;
      emitPacket("CryptHashData", context.id, this.backtrace, this.data, this.length);
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptGetHashParam"), {
    onEnter(args) {
      this.hash = args[0];
      this.data = args[2];
      this.dataLen = args[3];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.internal)
        return;
      const context = findHash(this.hash);
      if (context === null)
        return;
      emitPacket("CryptGetHashParam", context.id, this.backtrace, this.data, this.dataLen.readU32());
    },
  });

  Interceptor.attach(advapi32.getExportByName("CryptSetHashParam"), {
    onEnter(args) {
      this.hash = args[0];
      this.param = args[1].toUInt32();
      this.data = args[2];
      this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      if (retval.toInt32() === 0 || this.param !== HashParam.HmacInfo || this.internal)
        return;
      const context = findHash(this.hash);
      if (context === null)
        return;
      const hmac = serializeHmacInfo(this.data);
      emitPacket("CryptSetHashParam", context.id, this.backtrace, hmac.data, hmac.size);
    },
  });

  function instrumentCipher(name: string): void {
    Interceptor.attach(advapi32.getExportByName(name), {
      onEnter(args) {
        this.internal = calledInternally(this.returnAddress, CRYPT_INTERNAL_MODULES);
        if (this.internal)
          return;
        this.key = args[0];
        this.data = args[4];
        this.length = args[5];
        this.backtrace = captureBacktrace(this.context);
        reportCipher(name, this.key, "outgoing", this.data, this.length, this.backtrace);
      },
      onLeave() {
        if (this.internal)
          return;
        reportCipher(name, this.key, "incoming", this.data, this.length, this.backtrace);
      },
    });
  }

  function keyParamLength(param: number, key: NativePointer): number {
    if (param === KeyParam.IV)
      return blockLength(key) / 8;
    if (param === KeyParam.Permissions || param === KeyParam.AlgId || param === KeyParam.Padding ||
        param === KeyParam.Mode || param === KeyParam.ModeBits)
      return 4;
    return 0;
  }

  function blockLength(key: NativePointer): number {
    const value = Memory.alloc(4);
    const length = Memory.alloc(4);
    length.writeU32(4);

    queryingBlockLength = true;
    const result = cryptGetKeyParam(key, KeyParam.BlockLen, value, length, 0) as number;
    queryingBlockLength = false;

    return (result === 0) ? 0 : value.readU32();
  }
}

function reportCipher(name: string, key: NativePointer, direction: string, data: NativePointer, length: NativePointer, backtrace: string): void {
  if (data.isNull() || length.isNull())
    return;
  emitPacket(name, handleId(key), backtrace, data, length.readU32(), direction);
}

function reportSecurityBuffers(name: string, securityContext: NativePointer, message: NativePointer, direction: string, backtrace: string): void {
  if (message.isNull())
    return;

  const descriptor = SecBufferDesc.at(message);
  const buffers = descriptor.pBuffers;
  const id = contextId(securityContext);

  for (let i = 0; i !== descriptor.cBuffers; i++) {
    const buffer = SecBuffer.at(buffers.add(i * SecBuffer.size));
    if (buffer.BufferType === SECBUFFER_DATA && buffer.cbBuffer > 0)
      reportPacket(name, id, direction, "", "", backtrace, buffer.pvBuffer, buffer.cbBuffer);
  }
}

function serializeHmacInfo(address: NativePointer): { data: NativePointer; size: number } {
  const info = HMAC_INFO.at(address);
  const inner = info.cbInnerString;
  const outer = info.cbOuterString;
  const size = 12 + inner + outer;

  const buffer = Memory.alloc(size);
  buffer.writeU32(info.HashAlgid);
  buffer.add(4).writeU32(inner);
  Memory.copy(buffer.add(8), info.pbInnerString, inner);
  buffer.add(8 + inner).writeU32(outer);
  Memory.copy(buffer.add(12 + inner), info.pbOuterString, outer);
  return { data: buffer, size };
}

function emitPacket(name: string, resourceId: number, backtrace: string, data: NativePointer, length: number, direction: string = "invalid"): void {
  if (data.isNull() || length === 0)
    return;
  reportPacket(name, resourceId, direction, "", "", backtrace, data, length);
}

function calledInternally(returnAddress: NativePointer, suppressed: Set<string>): boolean {
  const module = Process.findModuleByAddress(returnAddress);
  return module !== null && suppressed.has(module.name.toLowerCase());
}

function algName(value: number): string {
  return AlgId[value] ?? ("0x" + hex32(value));
}

function hex32(value: number): string {
  return (value >>> 0).toString(16).padStart(8, "0");
}

function handleId(handle: NativePointer): number {
  return handle.toUInt32() >>> 0;
}

interface HashTracking {
  id: number;
  algId: number;
}

const hashContexts = new Map<string, HashTracking>();

function trackHash(hash: NativePointer, algId: number): HashTracking {
  const context = { id: newId(), algId };
  hashContexts.set(hash.toString(), context);
  return context;
}

function findHash(hash: NativePointer): HashTracking | null {
  return hashContexts.get(hash.toString()) ?? null;
}

function releaseHash(hash: NativePointer): void {
  hashContexts.delete(hash.toString());
}

const contextIds = new Map<string, number>();

function contextId(handle: NativePointer): number {
  if (handle.isNull())
    return 0;

  const key = handle.readU64().toString() + ":" + handle.add(8).readU64().toString();
  let id = contextIds.get(key);
  if (id === undefined) {
    id = newId();
    contextIds.set(key, id);
  }
  return id;
}

function releaseContextId(handle: NativePointer): void {
  if (handle.isNull())
    return;
  contextIds.delete(handle.readU64().toString() + ":" + handle.add(8).readU64().toString());
}

function newId(): number {
  return (Math.random() * 0xffffffff) >>> 0;
}

const CRYPT_INTERNAL_MODULES = new Set(["schannel.dll", "crypt32.dll"]);
const SECUR32_INTERNAL_MODULES = new Set(["rpcrt4.dll"]);
