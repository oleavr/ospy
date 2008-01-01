//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

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
CanonicalizeFilename (WCHAR * s)
{
  WCHAR * p = s;

  while (*p != '\0')
  {
    WCHAR c = *p;

    if (!((c >= 65 && c <= 90) ||
          (c >= 97 && c <= 122) ||
          (c >= 48 && c <= 57) ||
          c == '&'))
    {
      *p = '_';
    }

    p++;
  }
}

static NTSTATUS
AgentAddDevice (DRIVER_OBJECT * driverObject,
                DEVICE_OBJECT * physicalDeviceObject)
{
  NTSTATUS status;

  KdPrint (("AddDevice called with driverObject=%p, physicalDeviceObject=%p",
    driverObject, physicalDeviceObject));

  WCHAR hwId[256];
  ULONG hwIdLen;
  status = IoGetDeviceProperty (physicalDeviceObject, DevicePropertyHardwareID,
    sizeof (hwId), hwId, &hwIdLen);
  if (!NT_SUCCESS (status))
    return status;

  KdPrint (("DevicePropertyHardwareID = '%S'", hwId));

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

  CanonicalizeFilename (hwId);
  status = priv->logger.Start (&priv->removeLock, hwId);
  if (!NT_SUCCESS (status))
  {
    IoDeleteDevice (filterDeviceObject);
    return status;
  }

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
    priv->logger.Stop ();

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
  Event * ev = static_cast <Event *> (context);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);
  URB * urb = static_cast <URB *> (stackLocation->Parameters.Others.Argument1);

  if (irp->PendingReturned)
  {
    IoMarkIrpPending (irp);
  }

  Node * node = ev->CreateTextNode ("pendingReturned", 0, (irp->PendingReturned) ? "true" : "false");
  ev->AppendChild (node);

  priv->logger.SubmitEvent (ev);

  IoReleaseRemoveLock (&priv->removeLock, irp);

  return STATUS_SUCCESS;
}

static const char * urbFunctions [] =
{
  "URB_FUNCTION_SELECT_CONFIGURATION",
  "URB_FUNCTION_SELECT_INTERFACE",
  "URB_FUNCTION_ABORT_PIPE",
  "URB_FUNCTION_TAKE_FRAME_LENGTH_CONTROL",
  "URB_FUNCTION_RELEASE_FRAME_LENGTH_CONTROL",
  "URB_FUNCTION_GET_FRAME_LENGTH",
  "URB_FUNCTION_SET_FRAME_LENGTH",
  "URB_FUNCTION_GET_CURRENT_FRAME_NUMBER",
  "URB_FUNCTION_CONTROL_TRANSFER",
  "URB_FUNCTION_BULK_OR_INTERRUPT_TRANSFER",
  "URB_FUNCTION_ISOCH_TRANSFER",
  "URB_FUNCTION_GET_DESCRIPTOR_FROM_DEVICE",
  "URB_FUNCTION_SET_DESCRIPTOR_TO_DEVICE",
  "URB_FUNCTION_SET_FEATURE_TO_DEVICE",
  "URB_FUNCTION_SET_FEATURE_TO_INTERFACE",
  "URB_FUNCTION_SET_FEATURE_TO_ENDPOINT",
  "URB_FUNCTION_CLEAR_FEATURE_TO_DEVICE",
  "URB_FUNCTION_CLEAR_FEATURE_TO_INTERFACE",
  "URB_FUNCTION_CLEAR_FEATURE_TO_ENDPOINT",
  "URB_FUNCTION_GET_STATUS_FROM_DEVICE",
  "URB_FUNCTION_GET_STATUS_FROM_INTERFACE",
  "URB_FUNCTION_GET_STATUS_FROM_ENDPOINT",
  "URB_FUNCTION_RESERVED_0X0016",
  "URB_FUNCTION_VENDOR_DEVICE",
  "URB_FUNCTION_VENDOR_INTERFACE",
  "URB_FUNCTION_VENDOR_ENDPOINT",
  "URB_FUNCTION_CLASS_DEVICE",
  "URB_FUNCTION_CLASS_INTERFACE",
  "URB_FUNCTION_CLASS_ENDPOINT",
  "URB_FUNCTION_RESERVE_0X001D",
  "URB_FUNCTION_SYNC_RESET_PIPE_AND_CLEAR_STALL",
  "URB_FUNCTION_CLASS_OTHER",
  "URB_FUNCTION_VENDOR_OTHER",
  "URB_FUNCTION_GET_STATUS_FROM_OTHER",
  "URB_FUNCTION_CLEAR_FEATURE_TO_OTHER",
  "URB_FUNCTION_SET_FEATURE_TO_OTHER",
  "URB_FUNCTION_GET_DESCRIPTOR_FROM_ENDPOINT",
  "URB_FUNCTION_SET_DESCRIPTOR_TO_ENDPOINT",
  "URB_FUNCTION_GET_CONFIGURATION",
  "URB_FUNCTION_GET_INTERFACE",
  "URB_FUNCTION_GET_DESCRIPTOR_FROM_INTERFACE",
  "URB_FUNCTION_SET_DESCRIPTOR_TO_INTERFACE",
  "URB_FUNCTION_GET_MS_FEATURE_DESCRIPTOR",
  "URB_FUNCTION_RESERVE_0X002B",
  "URB_FUNCTION_RESERVE_0X002C",
  "URB_FUNCTION_RESERVE_0X002D",
  "URB_FUNCTION_RESERVE_0X002E",
  "URB_FUNCTION_RESERVE_0X002F",
  "URB_FUNCTION_SYNC_RESET_PIPE",
  "URB_FUNCTION_SYNC_CLEAR_STALL",
  "URB_FUNCTION_CONTROL_TRANSFER_EX",
  "URB_FUNCTION_SET_PIPE_IO_POLICY",
  "URB_FUNCTION_GET_PIPE_IO_POLICY", // 0x34
};

#define URB_FUNCTION_MAX 0x34

static NTSTATUS
AgentDispatchInternalIoctl (DEVICE_OBJECT * filterDeviceObject,
                            IRP * irp)
{
  AgentDeviceData * priv = static_cast <AgentDeviceData *>
    (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);

  ULONG controlCode = stackLocation->Parameters.DeviceIoControl.IoControlCode;

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return AgentCompleteRequest (irp, status);

  if (controlCode == IOCTL_INTERNAL_USB_SUBMIT_URB)
  {
    URB * urb = static_cast <URB *>
      (stackLocation->Parameters.Others.Argument1);

#if 0
    KdPrint ((
      "AgentDispatchInternalIoctl: IOCTL_INTERNAL_USB_SUBMIT_URB, urb=%p",
      urb));
#endif

    Event * ev = priv->logger.NewEvent ("IOCTL_INTERNAL_USB_SUBMIT_URB", 5);

    Node * node = NULL;
    if (urb->UrbHeader.Function <= URB_FUNCTION_MAX)
      node = ev->CreateTextNode ("type", 0, "%s", urbFunctions[urb->UrbHeader.Function]);
    else
      node = ev->CreateTextNode ("type", 0, "%d", urb->UrbHeader.Function);
    ev->AppendChild (node);

    if (urb->UrbHeader.Function == URB_FUNCTION_CLASS_INTERFACE)
    {
      const struct _URB_CONTROL_VENDOR_OR_CLASS_REQUEST * req =
        reinterpret_cast <const struct _URB_CONTROL_VENDOR_OR_CLASS_REQUEST *> (urb);

      node = ev->CreateTextNode ("direction", 0,
        (req->TransferFlags & USBD_TRANSFER_DIRECTION_IN) ? "in" : "out");
      ev->AppendChild (node);

      node = ev->CreateTextNode ("shortTransferOk", 0,
        (req->TransferFlags & USBD_SHORT_TRANSFER_OK) ? "true" : "false");
      ev->AppendChild (node);

      if (req->TransferBuffer != NULL)
      {
        node = ev->CreateDataNode ("transferBuffer", 1, req->TransferBuffer,
          req->TransferBufferLength);
      }
      else if (req->TransferBufferMDL != NULL)
      {
        node = ev->CreateDataNode ("transferBufferMDL", 1,
          MmGetSystemAddressForMdlSafe (req->TransferBufferMDL, HighPagePriority),
          req->TransferBufferLength);
      }
      else
      {
        node = NULL;
      }

      if (node != NULL)
      {
        ev->AddFieldToNodePrintf (node, "size", "%ld", req->TransferBufferLength);
        ev->AppendChild (node);
      }
    }

    IoCopyCurrentIrpStackLocationToNext (irp);
    IoSetCompletionRoutine (irp, AgentInternalIoctlCompletion, ev, TRUE,
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

static void AgentDriverUnload (DRIVER_OBJECT * driverObject);

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

  Logger::Initialize ();

  return STATUS_SUCCESS;
}

static void
AgentDriverUnload (DRIVER_OBJECT * driverObject)
{
  KdPrint (("AgentDriverUnload called with driverObject=%p", driverObject));

  Logger::Shutdown ();
}
