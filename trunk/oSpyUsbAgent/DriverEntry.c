#include <wdm.h>

NTSTATUS
DriverEntry (DRIVER_OBJECT * driver_object, WCHAR * registry_path)
{
  KdPrint (("Woot"));
  return STATUS_UNSUCCESSFUL;
}
