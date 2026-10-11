import { captureBacktrace, reportMessage } from "./events";
import { LayerType, PIXELFORMATDESCRIPTOR, PixelFormatFlags, PixelType } from "./pixelformat.hexpat";

export function instrumentGdi32(gdi32: Module): void {
  Interceptor.attach(gdi32.getExportByName("ChoosePixelFormat"), {
    onEnter(args) {
      this.hdc = args[0];
      this.pfd = args[1];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      reportPixelFormat("ChoosePixelFormat", this.hdc, this.backtrace,
        `hdc=0x${this.hdc.toString(16)} => ${retval.toInt32()}`, this.pfd);
    },
  });

  Interceptor.attach(gdi32.getExportByName("DescribePixelFormat"), {
    onEnter(args) {
      this.hdc = args[0];
      this.pixelFormat = args[1].toInt32();
      this.nBytes = args[2].toUInt32();
      this.pfd = args[3];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      const result = retval.toInt32();
      reportPixelFormat("DescribePixelFormat", this.hdc, this.backtrace,
        `hdc=0x${this.hdc.toString(16)} iPixelFormat=${this.pixelFormat} nBytes=${this.nBytes} => ${result}`,
        (result !== 0) ? this.pfd : NULL);
    },
  });

  Interceptor.attach(gdi32.getExportByName("GetPixelFormat"), {
    onEnter(args) {
      this.hdc = args[0];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      reportMessage("GetPixelFormat", this.hdc.toUInt32() >>> 0, "info", "", "", this.backtrace,
        `hdc=0x${this.hdc.toString(16)} => ${retval.toInt32()}`);
    },
  });

  Interceptor.attach(gdi32.getExportByName("SetPixelFormat"), {
    onEnter(args) {
      this.hdc = args[0];
      this.pixelFormat = args[1].toInt32();
      this.pfd = args[2];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      reportPixelFormat("SetPixelFormat", this.hdc, this.backtrace,
        `hdc=0x${this.hdc.toString(16)} iPixelFormat=${this.pixelFormat} => ${boolean(retval.toInt32())}`, this.pfd);
    },
  });

  Interceptor.attach(gdi32.getExportByName("SwapBuffers"), {
    onEnter(args) {
      this.hdc = args[0];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      reportMessage("SwapBuffers", this.hdc.toUInt32() >>> 0, "info", "", "", this.backtrace,
        `hdc=0x${this.hdc.toString(16)} => ${boolean(retval.toInt32())}`);
    },
  });
}

export function instrumentOpengl32(opengl32: Module): void {
  Interceptor.attach(opengl32.getExportByName("wglMakeCurrent"), {
    onEnter(args) {
      this.hdc = args[0];
      this.hglrc = args[1];
      this.backtrace = captureBacktrace(this.context);
    },
    onLeave(retval) {
      reportMessage("wglMakeCurrent", this.hglrc.toUInt32() >>> 0, "info", "", "", this.backtrace,
        `hdc=0x${this.hdc.toString(16)} hglrc=0x${this.hglrc.toString(16)} => ${boolean(retval.toInt32())}`);
    },
  });
}

function reportPixelFormat(name: string, hdc: NativePointer, backtrace: string, summary: string, pfd: NativePointer): void {
  const text = pfd.isNull() ? summary : `${summary}\r\n${describePixelFormat(pfd)}`;
  reportMessage(name, hdc.toUInt32() >>> 0, "info", "", "", backtrace, text);
}

function describePixelFormat(address: NativePointer): string {
  const pfd = PIXELFORMATDESCRIPTOR.parse(address);
  return [
    `nSize: ${pfd.nSize}`,
    `nVersion: ${pfd.nVersion}`,
    `dwFlags: ${flagNames(pfd.dwFlags)}`,
    `iPixelType: ${PixelType[pfd.iPixelType] ?? pfd.iPixelType}`,
    `cColorBits: ${pfd.cColorBits}`,
    `cRedBits: ${pfd.cRedBits}`,
    `cGreenBits: ${pfd.cGreenBits}`,
    `cBlueBits: ${pfd.cBlueBits}`,
    `cAlphaBits: ${pfd.cAlphaBits}`,
    `cAccumBits: ${pfd.cAccumBits}`,
    `cDepthBits: ${pfd.cDepthBits}`,
    `cStencilBits: ${pfd.cStencilBits}`,
    `cAuxBuffers: ${pfd.cAuxBuffers}`,
    `iLayerType: ${LayerType[pfd.iLayerType] ?? pfd.iLayerType}`,
  ].join("\r\n");
}

function flagNames(flags: PixelFormatFlags.Values): string {
  return Object.entries(flags)
    .filter(([name, value]) => value !== 0 && !name.startsWith("$"))
    .map(([name]) => name)
    .join(" | ");
}

function boolean(value: number): string {
  return (value !== 0) ? "TRUE" : "FALSE";
}
