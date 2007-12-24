#ifndef LOGGER_H
#define LOGGER_H

#include <wdm.h>

class Logger
{
public:
  Logger ();
  ~Logger ();

  void WriteRaw (void * data, size_t dataSize);
  void WriteLine (const WCHAR * format, ...);

private:
  HANDLE m_handle;
};

#endif // LOGGER_H