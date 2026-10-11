import { reportLookup, reportMessage } from "./events";

const CHALLENGE_SECRET = "80 7d 0c 00 68 b7 00 00 00";
const CONTACT_PROPERTY_ID_TO_NAME = [
  { startOffset: 0, pattern: "8b ff 55 8b ec 8b 45 08 83 f8 ?? 0f 87 ?? ?? ?? ?? ff 24 85 ?? ?? ?? ??" },
  { startOffset: -4, pattern: "5d c2 0c 00 55 8b ec 8b 45 08 83 f8 ?? 0f 87 ?? ?? ?? ?? ff 24 85 ?? ?? ?? ??" },
  { startOffset: -6, pattern: "5e e9 ?? ?? ?? ?? 55 8b ec 8b 45 08 83 f8 ?? 0f 87 ?? ?? ?? ?? ff 24 85 ?? ?? ?? ??" },
];

export function instrumentMessenger(): void {
  tryDump(dumpChallengeSecret);
  tryDump(dumpContactProperties);
}

function tryDump(dump: () => void): void {
  try {
    dump();
  } catch (e) {
    reportMessage("hook_msn", 0, "warning", "", "", "", `${(e as Error).message}`);
  }
}

export function instrumentWldlog(wldlog: Module): void {
  Interceptor.attach(wldlog.getExportByName("ZoneLoggingEnabled"), {
    onLeave(retval) {
      retval.replace(ptr(1));
    },
  });

  const expand = makeLogExpander();
  Interceptor.attach(wldlog.getExportByName("LogOutput"), {
    onEnter(args) {
      const message = expand(args[3], args[4]);
      if (message !== "")
        reportMessage("wldlog", 0, "log", "", "", "", message);
    },
  });
}

function makeLogExpander(): (format: NativePointer, varArgs: NativePointer) => string {
  const vsnwprintf = new NativeFunction(Process.getModuleByName("msvcrt.dll").getExportByName("_vsnwprintf"), "int", ["pointer", "size_t", "pointer", "pointer"], "mscdecl");

  return (format, varArgs) => {
    if (format.isNull())
      return "";

    const capacity = 1024;
    const buffer = Memory.alloc(capacity * 2);
    vsnwprintf(buffer, capacity - 1, format, varArgs);
    return buffer.readUtf16String()!;
  };
}

function dumpChallengeSecret(): void {
  const address = scanSignature("msnmsgr.exe", 16, CHALLENGE_SECRET);
  if (address === null)
    return;

  const getChallengeSecret = new NativeFunction(address, "pointer", ["pointer", "int"], "stdcall");
  const out = Memory.alloc(Process.pointerSize);

  getChallengeSecret(out, 1);
  const productId = out.readPointer().readCString();
  getChallengeSecret(out, 0);
  const productKey = out.readPointer().readCString();

  reportLookup("hook_msn", "", `Product ID: '${productId}'\r\nProduct Key: '${productKey}'`);
}

function dumpContactProperties(): void {
  let address: NativePointer | null = null;
  for (const signature of CONTACT_PROPERTY_ID_TO_NAME) {
    address = scanSignature("msnmsgr.exe", signature.startOffset, signature.pattern);
    if (address !== null)
      break;
  }
  if (address === null)
    return;

  const propertyIdToName = new NativeFunction(address, "pointer", ["int"], "stdcall");
  const lines: string[] = [];
  for (let id = 0; id !== 1024; id++) {
    const name = propertyIdToName(id) as NativePointer;
    if (!name.isNull())
      lines.push(`${id} => ${name.readUtf16String()}`);
  }

  reportLookup("hook_msn", "", lines.join("\r\n"));
}

function scanSignature(moduleName: string, startOffset: number, pattern: string): NativePointer | null {
  const module = Process.findModuleByName(moduleName);
  if (module === null)
    return null;

  const matches = Memory.scanSync(module.base, module.size, pattern);
  if (matches.length !== 1)
    return null;

  return matches[0].address.sub(startOffset);
}
