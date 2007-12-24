#include "Logger.h"

#include <wdm.h>

extern "C" NTSTATUS
DriverEntry (DRIVER_OBJECT * driverObject,
             UNICODE_STRING * registryPath)
{
  KdPrint (("Woot baby =)\n"));

  Logger logger;
  logger.WriteLine (L"Hey baby");
  logger.WriteLine (L"registryPath = '%s'", registryPath->Buffer);

  return STATUS_OPEN_FAILED;
}
