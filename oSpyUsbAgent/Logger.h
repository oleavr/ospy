#ifndef LOGGER_H
#define LOGGER_H

#include <wdm.h>

#pragma warning(push)
#pragma warning(disable:4200)
#include <usbdi.h>
#pragma warning(pop)

#define MAX_PATH 260

typedef struct {
    WCHAR LogPath[MAX_PATH];
    volatile ULONG LogIndexUserspace;
    volatile ULONG LogCount;
    volatile ULONG LogSize;
} Capture;

typedef struct {
  SLIST_ENTRY entry;
  ULONG id;
  LARGE_INTEGER timestamp;
  URB urb;
} UrbLogEntry;

class Logger
{
public:
  static void Initialize ();
  static void Shutdown ();

  NTSTATUS Start (IO_REMOVE_LOCK * removeLock, const WCHAR * fnSuffix);
  void Stop ();

  void LogUrb (const URB * urb);

private:
  static void LogThreadFuncWrapper (void * parameter) { static_cast <Logger *> (parameter)->LogThreadFunc (); }
  void LogThreadFunc ();

  void WriteUrbEntry (const UrbLogEntry * entry);

  void WriteRaw (const void * data, size_t dataSize);

  void Write (ULONG dw);
  void Write (const char * format, ...);

  static HANDLE m_captureSection;
  static Capture * m_capture;
  static volatile ULONG m_index;

  IO_REMOVE_LOCK * m_removeLock;

  HANDLE m_fileHandle;

  KEVENT m_stopEvent;
  HANDLE m_logThread;

  SLIST_HEADER m_items;
  KSPIN_LOCK m_itemsLock;
};

#endif // LOGGER_H