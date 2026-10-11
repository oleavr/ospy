import { instrumentCrypt, instrumentSecur32 } from "./crypto";
import { flush } from "./events";
import { instrumentHttpapi } from "./httpapi";
import { instrumentKernel32 } from "./kernel32";
import { instrumentMessenger, instrumentWldlog } from "./msn";
import { instrumentGdi32, instrumentOpengl32 } from "./opengl";
import { configureSoftwall, SoftwallRule } from "./softwall";
import { instrumentWininet } from "./wininet";
import { instrumentWinsock } from "./winsock";

rpc.exports = {
  configure(rules: SoftwallRule[]) {
    configureSoftwall(rules);
  },
  flush() {
    flush();
  },
};

Process.attachModuleObserver({
  onAdded(module) {
    switch (module.name.toLowerCase()) {
      case "ws2_32.dll":
        instrumentWinsock(module);
        break;
      case "wininet.dll":
        instrumentWininet(module);
        break;
      case "secur32.dll":
        instrumentSecur32(module);
        break;
      case "advapi32.dll":
        instrumentCrypt(module);
        break;
      case "kernel32.dll":
        instrumentKernel32(module);
        break;
      case "gdi32.dll":
        instrumentGdi32(module);
        break;
      case "opengl32.dll":
        instrumentOpengl32(module);
        break;
      case "httpapi.dll":
        instrumentHttpapi(module);
        break;
      case "wldlog.dll":
        instrumentWldlog(module);
        break;
    }
  },
});

if (Process.mainModule.name.toLowerCase() === "msnmsgr.exe")
  instrumentMessenger();
