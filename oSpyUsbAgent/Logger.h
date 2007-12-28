#ifndef LOGGER_H
#define LOGGER_H

#include <wdm.h>

#pragma warning(push)
#pragma warning(disable:4200)
#include <usbdi.h>
#pragma warning(pop)

typedef struct {
  SLIST_ENTRY entry;
  LARGE_INTEGER id;
  LARGE_INTEGER timestamp;
  URB urb;
} UrbLogEntry;

class Logger
{
public:
  NTSTATUS Initialize (IO_REMOVE_LOCK * removeLock, const WCHAR * fnSuffix);
  void Shutdown ();

  void LogUrb (const URB * urb);

private:
  static void LogThreadFuncWrapper (void * parameter) { static_cast <Logger *> (parameter)->LogThreadFunc (); }
  void LogThreadFunc ();

  void WriteUrbEntry (const UrbLogEntry * entry);

  void WriteRaw (const void * data, size_t dataSize);

  void Write (ULONG dw);
  void Write (const char * format, ...);

  IO_REMOVE_LOCK * m_removeLock;

  HANDLE m_fileHandle;

  KEVENT m_stopEvent;
  HANDLE m_logThread;

  LARGE_INTEGER m_index;
  KSPIN_LOCK m_indexLock;

  SLIST_HEADER m_items;
  KSPIN_LOCK m_itemsLock;
};

#endif // LOGGER_H