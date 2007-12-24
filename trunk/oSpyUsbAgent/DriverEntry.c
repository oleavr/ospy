#include <wdm.h>

NTSTATUS
DriverEntry (DRIVER_OBJECT * driver_object, WCHAR * registry_path)
{
  DbgPrint ("Woot baby\n");
  return STATUS_SUCCESS;
}
