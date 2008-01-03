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

#include "Agent.h"

#include "Logger.h"
#include "Urb.h"

namespace oSpy {

typedef struct {
  DEVICE_OBJECT * physicalDeviceObject;
  DEVICE_OBJECT * funcDeviceObject;
  DEVICE_OBJECT * filterDeviceObject;

  IO_REMOVE_LOCK removeLock;

  Logger logger;
} AgentDeviceData;

void
Agent::Initialize (DRIVER_OBJECT * driverObject)
{
  KdPrint (("oSpyUsbAgent snapshot (" __DATE__ " " __TIME__ ") initializing\n"));

  driverObject->DriverUnload = Agent::OnDriverUnload;
  driverObject->DriverExtension->AddDevice = Agent::OnAddDevice;

  int majorFunctionLength = sizeof (driverObject->MajorFunction)
    / sizeof (driverObject->MajorFunction[0]);
  for (int i = 0; i < majorFunctionLength; i++)
  {
    driverObject->MajorFunction[i] = Agent::OnAnyIrp;
  }

  driverObject->MajorFunction[IRP_MJ_POWER] = Agent::OnPowerIrp;
  driverObject->MajorFunction[IRP_MJ_PNP] = Agent::OnPnpIrp;
  driverObject->MajorFunction[IRP_MJ_INTERNAL_DEVICE_CONTROL] =
    Agent::OnInternalIoctlIrp;

  Logger::Initialize ();
}

void
Agent::OnDriverUnload (DRIVER_OBJECT * driverObject)
{
  KdPrint (("Agent::OnDriverUnload\n"));

  Logger::Shutdown ();
}

NTSTATUS
Agent::OnAddDevice (DRIVER_OBJECT * driverObject,
                    DEVICE_OBJECT * physicalDeviceObject)
{
  NTSTATUS status;

  KdPrint (("AddDevice called with driverObject=%p, physicalDeviceObject=%p\n",
    driverObject, physicalDeviceObject));

  WCHAR hwId[256];
  ULONG hwIdLen;
  status = IoGetDeviceProperty (physicalDeviceObject, DevicePropertyHardwareID,
    sizeof (hwId), hwId, &hwIdLen);
  if (!NT_SUCCESS (status))
    return status;

  KdPrint (("DevicePropertyHardwareID = '%S'\n", hwId));

  DEVICE_OBJECT * filterDeviceObject;
  status = IoCreateDevice (driverObject, sizeof (AgentDeviceData), NULL,
    FILE_DEVICE_UNKNOWN, 0, FALSE, &filterDeviceObject);
  if (!NT_SUCCESS (status))
  {
    KdPrint (("IoCreateDevice failed: 0x%08x\n", status));
    return status;
  }

  AgentDeviceData * priv =
    static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);

  IoInitializeRemoveLock (&priv->removeLock, 0, 1, 100);

  CanonicalizeFilename (hwId);
  status = priv->logger.Start (hwId);
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

NTSTATUS
Agent::OnAnyIrp (DEVICE_OBJECT * filterDeviceObject,
                 IRP * irp)
{
  AgentDeviceData * priv
    = static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);

  KdPrint (("Agent::OnDispatchAny\n"));

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return CompleteRequest (irp, status);

  IoSkipCurrentIrpStackLocation (irp);

  status = IoCallDriver (priv->funcDeviceObject, irp);

  IoReleaseRemoveLock (&priv->removeLock, irp);

  return status;
}

NTSTATUS
Agent::OnPowerIrp (DEVICE_OBJECT * filterDeviceObject,
                   IRP * irp)
{
  AgentDeviceData * priv
    = static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);

  KdPrint (("Agent::OnDispatchPower\n"));

  PoStartNextPowerIrp (irp); // should call IoCallDriver on Vista and newer

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return CompleteRequest (irp, status);

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

NTSTATUS
Agent::OnPnpIrp (DEVICE_OBJECT * filterDeviceObject,
                 IRP * irp)
{
  AgentDeviceData * priv =
    static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);

  KdPrint (("Agent::OnDispatchPnp: %s\n",
    PnpMinorFunctionToString (stackLocation->MinorFunction)));

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return CompleteRequest (irp, status);

  if (stackLocation->MinorFunction == IRP_MN_REMOVE_DEVICE)
  {
    KdPrint (("Agent::OnDispatchPnp: waiting to remove device\n"));

    priv->logger.Stop ();

    IoReleaseRemoveLockAndWait (&priv->removeLock, irp);

    KdPrint (("Agent::OnDispatchPnp: removing device\n"));

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

NTSTATUS
Agent::OnInternalIoctlIrp (DEVICE_OBJECT * filterDeviceObject,
                           IRP * irp)
{
  AgentDeviceData * priv = static_cast <AgentDeviceData *>
    (filterDeviceObject->DeviceExtension);
  IO_STACK_LOCATION * stackLocation = IoGetCurrentIrpStackLocation (irp);

  ULONG controlCode = stackLocation->Parameters.DeviceIoControl.IoControlCode;

  NTSTATUS status = IoAcquireRemoveLock (&priv->removeLock, irp);
  if (!NT_SUCCESS (status))
    return CompleteRequest (irp, status);

  if (controlCode == IOCTL_INTERNAL_USB_SUBMIT_URB)
  {
    URB * urb = static_cast <URB *>
      (stackLocation->Parameters.Others.Argument1);

    Event * ev = priv->logger.NewEvent ("IOCTL_INTERNAL_USB_SUBMIT_URB", 3, urb);

    Urb::AppendToNode (urb, ev, ev, true);

    IoCopyCurrentIrpStackLocationToNext (irp);
    IoSetCompletionRoutineEx (priv->filterDeviceObject, irp,
      OnUrbIoctlCompletion, ev, TRUE, TRUE, TRUE);
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

NTSTATUS
Agent::OnUrbIoctlCompletion (DEVICE_OBJECT * filterDeviceObject,
                             IRP * irp,
                             void * context)
{
  AgentDeviceData * priv =
    static_cast <AgentDeviceData *> (filterDeviceObject->DeviceExtension);
  Event * ev = static_cast <Event *> (context);
  URB * urb = static_cast <URB *> (ev->m_userData);

  if (irp->PendingReturned)
  {
    IoMarkIrpPending (irp);
  }

  Urb::AppendToNode (urb, ev, ev, false);

  Node * node = ev->CreateTextNode ("pendingReturned", 0,
    (irp->PendingReturned) ? "true" : "false");
  ev->AppendChild (node);

  priv->logger.SubmitEvent (ev);

  IoReleaseRemoveLock (&priv->removeLock, irp);

  return STATUS_CONTINUE_COMPLETION;
}

void
Agent::CanonicalizeFilename (WCHAR * s)
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

NTSTATUS
Agent::CompleteRequest (IRP * irp,
                        NTSTATUS status)
{
  irp->IoStatus.Status = status;
  irp->IoStatus.Information = 0;

  IoCompleteRequest (irp, IO_NO_INCREMENT);

  return status;
}

} // namespace oSpy