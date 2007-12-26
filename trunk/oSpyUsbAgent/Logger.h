#ifndef LOGGER_H
#define LOGGER_H

#include <wdm.h>

class Logger
{
public:
  NTSTATUS Initialize ();
  void Shutdown ();

  void WriteRaw (void * data, size_t dataSize);
  void WriteLine (const WCHAR * format, ...);

private:
  HANDLE m_fileHandle;
};

#endif // LOGGER_H