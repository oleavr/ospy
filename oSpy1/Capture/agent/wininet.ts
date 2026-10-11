import { captureBacktrace, reportPacket } from "./events";

export function instrumentWininet(wininet: Module): void {
  instrumentRequestBody("HttpSendRequestA");
  instrumentRequestBody("HttpSendRequestW");
  instrumentWrite("InternetWriteFile");
  instrumentRead("InternetReadFile");

  function instrumentRequestBody(name: string): void {
    Interceptor.attach(wininet.getExportByName(name), {
      onEnter(args) {
        reportBody(name, args[0], "outgoing", args[3], args[4].toUInt32(), captureBacktrace(this.context));
      },
    });
  }

  function instrumentWrite(name: string): void {
    Interceptor.attach(wininet.getExportByName(name), {
      onEnter(args) {
        reportBody(name, args[0], "outgoing", args[1], args[2].toUInt32(), captureBacktrace(this.context));
      },
    });
  }

  function instrumentRead(name: string): void {
    Interceptor.attach(wininet.getExportByName(name), {
      onEnter(args) {
        this.handle = args[0];
        this.buffer = args[1];
        this.read = args[3];
        this.backtrace = captureBacktrace(this.context);
      },
      onLeave(retval) {
        if (retval.toInt32() !== 0)
          reportBody(name, this.handle, "incoming", this.buffer, this.read.readU32(), this.backtrace);
      },
    });
  }
}

function reportBody(name: string, handle: NativePointer, direction: string, buffer: NativePointer, length: number, backtrace: string): void {
  if (buffer.isNull() || length === 0)
    return;

  reportPacket(name, handle.toUInt32() >>> 0, direction, "", "", backtrace, buffer, length);
}
