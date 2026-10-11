import { captureBacktrace, reportPacket } from "./events";

export function instrumentKernel32(kernel32: Module): void {
  Interceptor.attach(kernel32.getExportByName("DeviceIoControl"), {
    onEnter(args) {
      const input = args[2];
      const size = args[3].toUInt32();
      if (input.isNull() || size === 0)
        return;

      reportPacket("DeviceIoControl", args[0].toUInt32() >>> 0, "outgoing", "", "", captureBacktrace(this.context), input, size);
    },
  });
}
