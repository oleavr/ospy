#include "Logger.h"

#include <wdm.h>

#pragma warning(push)
#pragma warning(disable:4200)
#include <usbdi.h>
#pragma warning(pop)

typedef struct {
  DEVICE_OBJECT * physicalDeviceObject;
  DEVICE_OBJECT * funcDeviceObject;
  DEVICE_OBJECT * filterDeviceObject;

  IO_REMOVE_LOCK removeLock;

  Logger logger;
} AgentDeviceData;

static void
AgentDriverUnload (DRIVER_OBJECT * driverObject)
{
  KdPrint (("AgentDriverUnload called with driverObject=%p", driverObject));
}

static NTSTATUS
AgentAddDevice (DRIVER_OBJECT * driverObject,
                DEVICE_OBJECT * physicalDeviceObject)
{
  KdPrint (("AddDevice called with driverObject=%p, physicalDeviceObject=%p",
    driverObject, physicalDeviceObject));

  NTSTATUS status;
  DEVICE_OBJECT * filterDeviceObject;
  status = IoCreateDevice (driverObject, sizeof (AgentDeviceData), NULL,
    FILE_DEVICE_UNKNOWN, 0, FALSE, &filterDeviceObject);
  if (!NT_SUCCESS (status))
  {
    KdPrint (("IoCreateDevice failed: 0x%08x", status));
    return status;
  }

  AgentDeviceData * priv =
    static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);

  IoInitializeRemoveLock (&priv->removeLock, 0, 1, 100);

  //priv->logger.Initialize ();
  //IoGetDeviceProperty (physicalDeviceObject, DevicePropertyHardwareID, 

  DEVICE_OBJECT * funcDeviceObject =
    IoAttachDeviceToDeviceStack (filterDeviceObject, physicalDeviceObject);
  filterDeviceObject->Flags = funcDeviceObject->Flags &
    (DO_DIRECT_IO | DO_BUFFERED_IO | DO_POWER_PAGABLE | DO_POWER_INRUSH);
  filterDeviceObject->Flags &= ~DO_DEVICE_INITIALIZING;
  filterDeviceObject->DeviceType = funcDeviceObject->DeviceType;
  filterDeviceObject->Characteristics = funcDeviceObject->Characteristics;
  filterDeviceObject->AlignmentRequirement = funcDeviceObject->AlignmentRequirement;

  priv->funcDeviceObject = funcDeviceObject;
  priv->physicalDeviceObject = physicalDeviceObject;
  priv->filterDeviceObject = filterDeviceObject;

  return STATUS_SUCCESS;
}

static NTSTATUS
AgentCompleteRequest (IRP * irp, NTSTATUS status)
{
  irp->IoStatus.Status = status;
  irp->IoStatus.Information = 0;

  IoCompleteRequest (irp, IO_NO_INCREMENT);

  return status;
}

static NTSTATUS
AgentDispatchAny (DEVICE_OBJECT * filterDeviceObject,
                  IRP * irp)
{
  AgentDeviceData * priv
    = static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);
  
  KdPrint (("AgentDispatchAny: MajorFunction=%d", stackLocation->MajorFunction));

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return AgentCompleteRequest (irp, status);

  IoSkipCurrentIrpStackLocation (irp);

  status = IoCallDriver (priv->funcDeviceObject, irp);

  IoReleaseRemoveLock (&priv->removeLock, irp);

  return status;
}

static NTSTATUS
AgentDispatchPower (DEVICE_OBJECT * filterDeviceObject,
                    IRP * irp)
{
  AgentDeviceData * priv
    = static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);

  KdPrint (("AgentDispatchPower"));

  PoStartNextPowerIrp (irp); // should call IoCallDriver on Vista and newer

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return AgentCompleteRequest (irp, status);

  IoSkipCurrentIrpStackLocation (irp);

  status = PoCallDriver (priv->funcDeviceObject, irp);

  IoReleaseRemoveLock (&priv->removeLock, irp);

  return status;
}

static const char *
PnpMinorFunctionToString (UCHAR minorFunction)
{
  const char * pnpMinorFunctions[] = {
    "IRP_MN_START_DEVICE",
    "IRP_MN_QUERY_REMOVE_DEVICE",
    "IRP_MN_REMOVE_DEVICE",
    "IRP_MN_CANCEL_REMOVE_DEVICE",
    "IRP_MN_STOP_DEVICE",
    "IRP_MN_QUERY_STOP_DEVICE",
    "IRP_MN_CANCEL_STOP_DEVICE",

    "IRP_MN_QUERY_DEVICE_RELATIONS",
    "IRP_MN_QUERY_INTERFACE",
    "IRP_MN_QUERY_CAPABILITIES",
    "IRP_MN_QUERY_RESOURCES",
    "IRP_MN_QUERY_RESOURCE_REQUIREMENTS",
    "IRP_MN_QUERY_DEVICE_TEXT",
    "IRP_MN_FILTER_RESOURCE_REQUIREMENTS",

    "",

    "IRP_MN_READ_CONFIG",
    "IRP_MN_WRITE_CONFIG",
    "IRP_MN_EJECT",
    "IRP_MN_SET_LOCK",
    "IRP_MN_QUERY_ID",
    "IRP_MN_QUERY_PNP_DEVICE_STATE",
    "IRP_MN_QUERY_BUS_INFORMATION",
    "IRP_MN_DEVICE_USAGE_NOTIFICATION",
    "IRP_MN_SURPRISE_REMOVAL", // 0x17
  };

  if (minorFunction <= 0x17)
    return pnpMinorFunctions[minorFunction];
  else
    return "IRP_MN_UNKNOWN_PNP_FUNCTION";
}

static NTSTATUS
AgentDispatchPnp (DEVICE_OBJECT * filterDeviceObject,
                  IRP * irp)
{
  AgentDeviceData * priv =
    static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);

  KdPrint (("AgentDispatchPnp %s: irp=%p",
    PnpMinorFunctionToString (stackLocation->MinorFunction), irp));

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return AgentCompleteRequest (irp, status);

  if (stackLocation->MinorFunction == IRP_MN_REMOVE_DEVICE)
  {
    KdPrint (("AgentDispatchPnp: waiting to remove device"));

    IoReleaseRemoveLockAndWait (&priv->removeLock, irp);

    KdPrint (("AgentDispatchPnp: removing device"));

    IoSkipCurrentIrpStackLocation (irp);

    status = IoCallDriver (priv->funcDeviceObject, irp);

    IoDetachDevice (priv->funcDeviceObject);
    IoDeleteDevice (filterDeviceObject);
  }
  else
  {
    IoSkipCurrentIrpStackLocation (irp);
    status = IoCallDriver (priv->funcDeviceObject, irp);
    IoReleaseRemoveLock (&priv->removeLock, irp);
  }

  return status;
}

static NTSTATUS
AgentInternalIoctlCompletion (DEVICE_OBJECT * filterDeviceObject,
                              IRP * irp,
                              void * context)
{
  AgentDeviceData * priv =
    static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);

  KdPrint (("AgentInternalIoctlCompletion: irp=%p", irp));

  if (irp->PendingReturned)
  {
    KdPrint (("irp->PendingReturned"));
    IoMarkIrpPending (irp);
  }

  IoReleaseRemoveLock (&priv->removeLock, irp);

  return STATUS_SUCCESS;
}

static NTSTATUS
AgentDispatchInternalIoctl (DEVICE_OBJECT * filterDeviceObject,
                            IRP * irp)
{
  AgentDeviceData * priv = static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);
  
  ULONG controlCode = stackLocation->Parameters.DeviceIoControl.IoControlCode;
  KdPrint (("AgentDispatchInternalIoctl: irp=%p, IoControlCode=%d", irp, controlCode));

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return AgentCompleteRequest (irp, status);

  if (controlCode == IOCTL_INTERNAL_USB_SUBMIT_URB)
  {
    URB * urb = static_cast <URB *>
      (stackLocation->Parameters.Others.Argument1);

    KdPrint ((
      "AgentDispatchInternalIoctl: IOCTL_INTERNAL_USB_SUBMIT_URB, urb=%p",
      urb));

    IoCopyCurrentIrpStackLocationToNext (irp);
    IoSetCompletionRoutine (irp, AgentInternalIoctlCompletion, NULL, TRUE,
      TRUE, TRUE);
  }
  else
  {
    IoSkipCurrentIrpStackLocation (irp);
  }

  status = IoCallDriver (priv->funcDeviceObject, irp);

  if (controlCode != IOCTL_INTERNAL_USB_SUBMIT_URB)
    IoReleaseRemoveLock (&priv->removeLock, irp);

  return status;
}

extern "C" NTSTATUS
DriverEntry (DRIVER_OBJECT * driverObject,
             UNICODE_STRING * registryPath)
{
  KdPrint (("oSpyUsbAgent snapshot (" __DATE__ " " __TIME__ ") initializing\n"));

  driverObject->DriverUnload = AgentDriverUnload;
  driverObject->DriverExtension->AddDevice = AgentAddDevice;

  int majorFunctionLength = sizeof (driverObject->MajorFunction)
    / sizeof (driverObject->MajorFunction[0]);
  for (int i = 0; i < majorFunctionLength; i++)
  {
    driverObject->MajorFunction[i] = AgentDispatchAny;
  }

  driverObject->MajorFunction[IRP_MJ_POWER] = AgentDispatchPower;
  driverObject->MajorFunction[IRP_MJ_PNP] = AgentDispatchPnp;
  driverObject->MajorFunction[IRP_MJ_INTERNAL_DEVICE_CONTROL] = AgentDispatchInternalIoctl;

  return STATUS_SUCCESS;
}
