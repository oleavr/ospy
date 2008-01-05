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

#include "Urb.h"

#include "Event.h"

namespace oSpy {

const UrbFunctionParser Urb::functionParsers[] =
{
  ParseSelectConfiguration,         // URB_FUNCTION_SELECT_CONFIGURATION
  ParseSelectInterface,             // URB_FUNCTION_SELECT_INTERFACE
  ParseAbortPipe,                   // URB_FUNCTION_ABORT_PIPE
  NULL,                             // URB_FUNCTION_TAKE_FRAME_LENGTH_CONTROL
  NULL,                             // URB_FUNCTION_RELEASE_FRAME_LENGTH_CONTROL
  NULL,                             // URB_FUNCTION_GET_FRAME_LENGTH
  NULL,                             // URB_FUNCTION_SET_FRAME_LENGTH
  ParseGetCurrentFrameNumber,       // URB_FUNCTION_GET_CURRENT_FRAME_NUMBER
  ParseControlTransfer,             // URB_FUNCTION_CONTROL_TRANSFER
  ParseBulkOrInterruptTransfer,     // URB_FUNCTION_BULK_OR_INTERRUPT_TRANSFER
  ParseIsochTransfer,               // URB_FUNCTION_ISOCH_TRANSFER
  ParseGetDescriptorFromDevice,     // URB_FUNCTION_GET_DESCRIPTOR_FROM_DEVICE
  NULL,                             // URB_FUNCTION_SET_DESCRIPTOR_TO_DEVICE
  NULL,                             // URB_FUNCTION_SET_FEATURE_TO_DEVICE
  NULL,                             // URB_FUNCTION_SET_FEATURE_TO_INTERFACE
  NULL,                             // URB_FUNCTION_SET_FEATURE_TO_ENDPOINT
  NULL,                             // URB_FUNCTION_CLEAR_FEATURE_TO_DEVICE
  NULL,                             // URB_FUNCTION_CLEAR_FEATURE_TO_INTERFACE
  NULL,                             // URB_FUNCTION_CLEAR_FEATURE_TO_ENDPOINT
  NULL,                             // URB_FUNCTION_GET_STATUS_FROM_DEVICE
  NULL,                             // URB_FUNCTION_GET_STATUS_FROM_INTERFACE
  NULL,                             // URB_FUNCTION_GET_STATUS_FROM_ENDPOINT
  NULL,                             // URB_FUNCTION_RESERVED_0X0016
  NULL,                             // URB_FUNCTION_VENDOR_DEVICE
  NULL,                             // URB_FUNCTION_VENDOR_INTERFACE
  NULL,                             // URB_FUNCTION_VENDOR_ENDPOINT
  NULL,                             // URB_FUNCTION_CLASS_DEVICE
  ParseClassInterface,              // URB_FUNCTION_CLASS_INTERFACE
  NULL,                             // URB_FUNCTION_CLASS_ENDPOINT
  NULL,                             // URB_FUNCTION_RESERVE_0X001D
  ParseSyncResetPipeAndClearStall,  // URB_FUNCTION_SYNC_RESET_PIPE_AND_CLEAR_STALL
  NULL,                             // URB_FUNCTION_CLASS_OTHER
  NULL,                             // URB_FUNCTION_VENDOR_OTHER
  NULL,                             // URB_FUNCTION_GET_STATUS_FROM_OTHER
  NULL,                             // URB_FUNCTION_CLEAR_FEATURE_TO_OTHER
  NULL,                             // URB_FUNCTION_SET_FEATURE_TO_OTHER
  NULL,                             // URB_FUNCTION_GET_DESCRIPTOR_FROM_ENDPOINT
  NULL,                             // URB_FUNCTION_SET_DESCRIPTOR_TO_ENDPOINT
  NULL,                             // URB_FUNCTION_GET_CONFIGURATION
  NULL,                             // URB_FUNCTION_GET_INTERFACE
  NULL,                             // URB_FUNCTION_GET_DESCRIPTOR_FROM_INTERFACE
  NULL,                             // URB_FUNCTION_SET_DESCRIPTOR_TO_INTERFACE
  NULL,                             // URB_FUNCTION_GET_MS_FEATURE_DESCRIPTOR
  NULL,                             // URB_FUNCTION_RESERVE_0X002B
  NULL,                             // URB_FUNCTION_RESERVE_0X002C
  NULL,                             // URB_FUNCTION_RESERVE_0X002D
  NULL,                             // URB_FUNCTION_RESERVE_0X002E
  NULL,                             // URB_FUNCTION_RESERVE_0X002F
  NULL,                             // URB_FUNCTION_SYNC_RESET_PIPE
  NULL,                             // URB_FUNCTION_SYNC_CLEAR_STALL
  NULL,                             // URB_FUNCTION_CONTROL_TRANSFER_EX
  NULL,                             // URB_FUNCTION_SET_PIPE_IO_POLICY
  NULL,                             // URB_FUNCTION_GET_PIPE_IO_POLICY // 0x34
};

#define URB_FUNCTION_MAX 0x34

void
Urb::AppendToNode (const URB * urb,
                   Event * ev,
                   Node * parentNode,
                   bool onEntry)
{
  Node * urbNode = ev->CreateElement ("urb", 1, 10);
  ev->AddFieldToNode (urbNode, "direction", (onEntry) ? "in" : "out");
  parentNode->AppendChild (urbNode);

  Node * node;

  const char * funcStr = FunctionToString (urb->UrbHeader.Function);
  node = ev->CreateTextNode ("type", 0, "%s", funcStr);
  urbNode->AppendChild (node);

  USHORT function = urb->UrbHeader.Function;
  if (function <= URB_FUNCTION_MAX && functionParsers[function] != NULL)
  {
    functionParsers[function] (urb, ev, urbNode, onEntry);
  }
  else
  {
    KdPrint (("%s needs to be handled\n", funcStr));
    node = ev->CreateTextNode ("FIXME", 0, "This URB is not yet handled.");
    urbNode->AppendChild (node);
  }

  if (!onEntry)
  {
    node = ev->CreateTextNode ("status", 1, "%s", StatusToString (
      urb->UrbHeader.Status));
    ev->AddFieldToNodePrintf (node, "code", "0x%08x", urb->UrbHeader.Status);
    urbNode->AppendChild (node);
  }
}

void
Urb::ParseSelectConfiguration (const URB * urb,
                               Event * ev,
                               Node * parentNode,
                               bool onEntry)
{
  const struct _URB_SELECT_CONFIGURATION * sel =
    reinterpret_cast <const struct _URB_SELECT_CONFIGURATION *> (urb);

  AppendConfigDescriptorToNode (sel->ConfigurationDescriptor, ev, parentNode);

  Node * node = ev->CreateTextNode ("configurationHandle", 0, "0x%p",
    sel->ConfigurationHandle);
  parentNode->AppendChild (node);
}

void
Urb::ParseSelectInterface (const URB * urb,
                           Event * ev,
                           Node * parentNode,
                           bool onEntry)
{
  const struct _URB_SELECT_INTERFACE * sel =
    reinterpret_cast <const struct _URB_SELECT_INTERFACE *> (urb);

  Node * node = ev->CreateTextNode ("configurationHandle", 0, "0x%p",
    sel->ConfigurationHandle);
  parentNode->AppendChild (node);

  AppendInterfaceInfoToNode (&sel->Interface, ev, parentNode);
}

void
Urb::ParseAbortPipe (const URB * urb,
                     Event * ev,
                     Node * parentNode,
                     bool onEntry)
{
  const struct _URB_PIPE_REQUEST * req =
    reinterpret_cast <const struct _URB_PIPE_REQUEST *> (urb);

  Node * node = ev->CreateTextNode ("pipeHandle", 0, "0x%p",
    req->PipeHandle);
  parentNode->AppendChild (node);
}

void
Urb::ParseGetCurrentFrameNumber (const URB * urb,
                                 Event * ev,
                                 Node * parentNode,
                                 bool onEntry)
{
  const struct _URB_GET_CURRENT_FRAME_NUMBER * get =
    reinterpret_cast <const struct _URB_GET_CURRENT_FRAME_NUMBER *> (urb);

  Node * node = ev->CreateTextNode ("frameNumber", 0, "%d", get->FrameNumber);
  parentNode->AppendChild (node);
}

void
Urb::ParseControlTransfer (const URB * urb,
                           Event * ev,
                           Node * parentNode,
                           bool onEntry)
{
  const struct _URB_CONTROL_TRANSFER * xfer =
    reinterpret_cast <const struct _URB_CONTROL_TRANSFER *> (urb);

  Node * node = ev->CreateTextNode ("pipeHandle", 0, "0x%p", xfer->PipeHandle);
  parentNode->AppendChild (node);

  AppendTransferFlagsToNode (xfer->TransferFlags, ev, parentNode);

  node = ev->CreateTextNode ("urbLink", 0, "%p", xfer->UrbLink);
  parentNode->AppendChild (node);

  node = ev->CreateDataNode ("setupPacket", 1, xfer->SetupPacket,
    sizeof (xfer->SetupPacket));
  ev->AddFieldToNodePrintf (node, "size", "%ld", sizeof (xfer->SetupPacket));
  parentNode->AppendChild (node);

  AppendTransferBufferToNode (xfer->TransferBuffer,
    xfer->TransferBufferLength, xfer->TransferBufferMDL, ev, parentNode);
}

void
Urb::ParseBulkOrInterruptTransfer (const URB * urb,
                                   Event * ev,
                                   Node * parentNode,
                                   bool onEntry)
{
  const struct _URB_BULK_OR_INTERRUPT_TRANSFER * xfer =
    reinterpret_cast <const struct _URB_BULK_OR_INTERRUPT_TRANSFER *> (urb);

  Node * node = ev->CreateTextNode ("pipeHandle", 0, "0x%p", xfer->PipeHandle);
  parentNode->AppendChild (node);

  AppendTransferFlagsToNode (xfer->TransferFlags, ev, parentNode);

  AppendTransferBufferToNode (xfer->TransferBuffer, xfer->TransferBufferLength,
    xfer->TransferBufferMDL, ev, parentNode);

  node = ev->CreateTextNode ("urbLink", 0, "0x%p", xfer->UrbLink);
  parentNode->AppendChild (node);
}

void
Urb::ParseIsochTransfer (const URB * urb,
                         Event * ev,
                         Node * parentNode,
                         bool onEntry)
{
  const struct _URB_ISOCH_TRANSFER * xfer =
    reinterpret_cast <const struct _URB_ISOCH_TRANSFER *> (urb);

  Node * node = ev->CreateTextNode ("pipeHandle", 0, "0x%p", xfer->PipeHandle);
  parentNode->AppendChild (node);

  AppendTransferFlagsToNode (xfer->TransferFlags, ev, parentNode);

  bool dumpData =
    (xfer->TransferFlags & USBD_TRANSFER_DIRECTION_IN && !onEntry) ||
    (xfer->TransferFlags & USBD_TRANSFER_DIRECTION_OUT && onEntry);

  node = ev->CreateTextNode ("startFrame", 0, "%d", xfer->StartFrame);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("errorCount", 0, "%d", xfer->ErrorCount);
  parentNode->AppendChild (node);

  int pktCount = xfer->NumberOfPackets;
  if (!onEntry)
  {
    pktCount = 0;

    for (ULONG i = 0; i < xfer->NumberOfPackets; i++)
    {
      const USBD_ISO_PACKET_DESCRIPTOR * pkt = &xfer->IsoPacket[i];

      if (pkt->Status == USBD_STATUS_SUCCESS && pkt->Length > 0)
        pktCount++;
    }
  }

  Node * packetsNode = ev->CreateElement ("isoPackets", 1, pktCount);
  ev->AddFieldToNodePrintf (packetsNode, "count", "%d", xfer->NumberOfPackets);
  parentNode->AppendChild (packetsNode);

  const UCHAR * xferBuffer = NULL;
  if (xfer->TransferBuffer != NULL)
    xferBuffer = static_cast <UCHAR *> (xfer->TransferBuffer);
  else if (xfer->TransferBufferMDL != NULL)
    xferBuffer = static_cast <UCHAR *> (MmGetSystemAddressForMdlSafe
      (xfer->TransferBufferMDL, HighPagePriority));

  if (xferBuffer != NULL)
  {
    for (ULONG i = 0; i < xfer->NumberOfPackets; i++)
    {
      const USBD_ISO_PACKET_DESCRIPTOR * pkt = &xfer->IsoPacket[i];

      if (onEntry || (pkt->Status == USBD_STATUS_SUCCESS && pkt->Length > 0))
      {
        if (dumpData)
          node = ev->CreateDataNode ("packet", 3, xferBuffer + pkt->Offset, pkt->Length);
        else
          node = ev->CreateElement ("packet", 3);

        ev->AddFieldToNodePrintf (node, "index", "%d", i);
        ev->AddFieldToNodePrintf (node, "offset", "%d", pkt->Offset);
        ev->AddFieldToNodePrintf (node, "size", "%d", pkt->Length);

        packetsNode->AppendChild (node);
      }
    }
  }
}

void
Urb::ParseGetDescriptorFromDevice (const URB * urb,
                                   Event * ev,
                                   Node * parentNode,
                                   bool onEntry)
{
  const struct _URB_CONTROL_DESCRIPTOR_REQUEST * req =
    reinterpret_cast <const struct _URB_CONTROL_DESCRIPTOR_REQUEST *>
    (urb);

  Node * node = ev->CreateTextNode ("index", 0, "%d", req->Index);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("descriptorType", 0,
    DescriptorTypeToString (req->DescriptorType));
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("languageId", 0, "0x%x", req->LanguageId);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("urbLink", 0, "0x%p", req->UrbLink);
  parentNode->AppendChild (node);

  AppendTransferBufferToNode (req->TransferBuffer,
    req->TransferBufferLength, req->TransferBufferMDL, ev, parentNode);
}

void
Urb::ParseClassInterface (const URB * urb,
                          Event * ev,
                          Node * parentNode,
                          bool onEntry)
{
  const struct _URB_CONTROL_VENDOR_OR_CLASS_REQUEST * req =
    reinterpret_cast <const struct _URB_CONTROL_VENDOR_OR_CLASS_REQUEST *> (urb);

  AppendTransferFlagsToNode (req->TransferFlags, ev, parentNode);

  Node * node = ev->CreateTextNode ("urbLink", 0, "0x%p", req->UrbLink);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("requestTypeReservedBits", 0, "%d",
    req->RequestTypeReservedBits);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("request", 0, "0x%02x", req->Request);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("value", 0, "0x%04x", req->Value);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("index", 0, "0x%04x", req->Index);
  parentNode->AppendChild (node);

  AppendTransferBufferToNode (req->TransferBuffer,
    req->TransferBufferLength, req->TransferBufferMDL, ev, parentNode);
}

void
Urb::ParseSyncResetPipeAndClearStall (const URB * urb,
                                      Event * ev,
                                      Node * parentNode,
                                      bool onEntry)
{
  const struct _URB_PIPE_REQUEST * req =
    reinterpret_cast <const struct _URB_PIPE_REQUEST *> (urb);

  Node * node = ev->CreateTextNode ("pipeHandle", 0, "0x%p", req->PipeHandle);
  parentNode->AppendChild (node);
}

void
Urb::AppendConfigDescriptorToNode (const USB_CONFIGURATION_DESCRIPTOR * desc,
                                   Event * ev,
                                   Node * parentNode)
{
  Node * descNode = ev->CreateElement ("configurationDescriptor", 1, 8);
  ev->AddFieldToNodePrintf (descNode, "value", "0x%p", desc);
  parentNode->AppendChild (descNode);

  if (desc != NULL)
  {
    Node * node;

    node = ev->CreateTextNode ("bLength", 0, "%d", desc->bLength);
    descNode->AppendChild (node);

    node = ev->CreateTextNode ("bDescriptorType", 0,
      DescriptorTypeToString (desc->bDescriptorType));
    descNode->AppendChild (node);

    node = ev->CreateTextNode ("wTotalLength", 0, "%d", desc->wTotalLength);
    descNode->AppendChild (node);

    node = ev->CreateTextNode ("bNumInterfaces", 0, "%d",
      desc->bNumInterfaces);
    descNode->AppendChild (node);

    node = ev->CreateTextNode ("iConfiguration", 0, "%d",
      desc->iConfiguration);
    descNode->AppendChild (node);

    node = ev->CreateTextNode ("bConfigurationValue", 0, "%d",
      desc->bConfigurationValue);
    descNode->AppendChild (node);

    Node * attrsNode = ev->CreateElement ("bmAttributes", 1, 3);
    ev->AddFieldToNodePrintf (attrsNode, "value", "0x%02x",
      desc->bmAttributes);

    if (desc->bmAttributes & 32)
    {
      node = ev->CreateElement ("remoteWakup");
      attrsNode->AppendChild (node);
    }

    if (desc->bmAttributes & 64)
    {
      node = ev->CreateElement ("selfPowered");
      attrsNode->AppendChild (node);
    }

    if (desc->bmAttributes & 128)
    {
      node = ev->CreateElement ("busPowered");
      attrsNode->AppendChild (node);
    }

    descNode->AppendChild (attrsNode);

    node = ev->CreateTextNode ("MaxPower", 1, "%d", desc->MaxPower * 2);
    ev->AddFieldToNodePrintf (node, "unit", "mA");
    descNode->AppendChild (node);
  }
}

void
Urb::AppendInterfaceInfoToNode (const USBD_INTERFACE_INFORMATION * info,
                                Event * ev,
                                Node * parentNode)
{
  Node * infoNode = ev->CreateElement ("interfaceInformation", 1, 8);
  ev->AddFieldToNodePrintf (infoNode, "value", "0x%p", info);
  parentNode->AppendChild (infoNode);

  if (info != NULL)
  {
    Node * node;

    node = ev->CreateTextNode ("Length", 0, "%d", info->Length);
    infoNode->AppendChild (node);

    node = ev->CreateTextNode ("InterfaceNumber", 0, "%d",
      info->InterfaceNumber);
    infoNode->AppendChild (node);

    node = ev->CreateTextNode ("AlternateSetting", 0, "%d",
      info->AlternateSetting);
    infoNode->AppendChild (node);

    node = ev->CreateTextNode ("Class", 0, "0x%02x", info->Class);
    infoNode->AppendChild (node);

    node = ev->CreateTextNode ("SubClass", 0, "0x%02x", info->SubClass);
    infoNode->AppendChild (node);

    node = ev->CreateTextNode ("Protocol", 0, "0x%02x", info->Protocol);
    infoNode->AppendChild (node);

    node = ev->CreateTextNode ("InterfaceHandle", 0, "0x%p",
      info->InterfaceHandle);
    infoNode->AppendChild (node);

    Node * pipesNode = ev->CreateElement ("Pipes", 1, info->NumberOfPipes);
    ev->AddFieldToNodePrintf (pipesNode, "count", "%d", info->NumberOfPipes);
    infoNode->AppendChild (pipesNode);

    for (ULONG i = 0; i < info->NumberOfPipes; i++)
    {
      AppendPipeInfoToNode (&info->Pipes[i], ev, pipesNode);
    }
  }
}

void
Urb::AppendPipeInfoToNode (const USBD_PIPE_INFORMATION * info,
                           Event * ev,
                           Node * parentNode)
{
  Node * pipeNode = ev->CreateElement ("Pipe", 1, 8);
  ev->AddFieldToNodePrintf (pipeNode, "value", "0x%p", info);
  parentNode->AppendChild (pipeNode);

  if (info != NULL)
  {
    Node * node;

    node = ev->CreateTextNode ("MaximumPacketSize", 0, "%d",
      info->MaximumPacketSize);
    pipeNode->AppendChild (node);

    node = ev->CreateTextNode ("EndpointAddress", 0, "0x%02x",
      info->EndpointAddress);
    pipeNode->AppendChild (node);

    node = ev->CreateTextNode ("Interval", 0, "%d", info->Interval);
    pipeNode->AppendChild (node);

    node = ev->CreateTextNode ("Interval", 0, "%d", info->Interval);
    pipeNode->AppendChild (node);

    node = ev->CreateTextNode ("PipeType", 0,
      PipeTypeToString (info->PipeType));
    pipeNode->AppendChild (node);

    node = ev->CreateTextNode ("PipeHandle", 0, "0x%p", info->PipeHandle);
    pipeNode->AppendChild (node);

    node = ev->CreateTextNode ("MaximumTransferSize", 0, "%d",
      info->MaximumTransferSize);
    pipeNode->AppendChild (node);

    Node * flagsNode = ev->CreateElement ("PipeFlags", 1, 4);
    ev->AddFieldToNodePrintf (flagsNode, "value", "0x%08x", info->PipeFlags);
    pipeNode->AppendChild (flagsNode);

    if (info->PipeFlags & USBD_PF_CHANGE_MAX_PACKET)
    {
      node = ev->CreateElement ("USBD_PF_CHANGE_MAX_PACKET");
      flagsNode->AppendChild (node);
    }

    if (info->PipeFlags & USBD_PF_SHORT_PACKET_OPT)
    {
      node = ev->CreateElement ("USBD_PF_SHORT_PACKET_OPT");
      flagsNode->AppendChild (node);
    }

    if (info->PipeFlags & USBD_PF_ENABLE_RT_THREAD_ACCESS)
    {
      node = ev->CreateElement ("USBD_PF_ENABLE_RT_THREAD_ACCESS");
      flagsNode->AppendChild (node);
    }

    if (info->PipeFlags & USBD_PF_MAP_ADD_TRANSFERS)
    {
      node = ev->CreateElement ("USBD_PF_MAP_ADD_TRANSFERS");
      flagsNode->AppendChild (node);
    }     
  }
}

void
Urb::AppendTransferFlagsToNode (ULONG flags,
                                Event * ev,
                                Node * parentNode)
{
  Node * node = ev->CreateTextNode ("direction", 0,
    (flags & USBD_TRANSFER_DIRECTION_IN) ? "in" : "out");
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("shortTransferOk", 0,
    (flags & USBD_SHORT_TRANSFER_OK) ? "true" : "false");
  parentNode->AppendChild (node);
}

void
Urb::AppendTransferBufferToNode (const void * transferBuffer,
                                 int transferBufferLength,
                                 MDL * transferBufferMDL,
                                 Event * ev,
                                 Node * parentNode)
{
  Node * node = NULL;

  if (transferBuffer != NULL)
  {
    node = ev->CreateDataNode ("transferBuffer", 1, transferBuffer,
      transferBufferLength);
  }
  else if (transferBufferMDL != NULL)
  {
    node = ev->CreateDataNode ("transferBufferMDL", 1,
      MmGetSystemAddressForMdlSafe (transferBufferMDL, HighPagePriority),
      transferBufferLength);
  }

  if (node != NULL)
  {
    ev->AddFieldToNodePrintf (node, "size", "%ld", transferBufferLength);
    parentNode->AppendChild (node);
  }
}

static const char * functions [] =
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

const char *
Urb::FunctionToString (USHORT function)
{
  if (function <= URB_FUNCTION_MAX)
    return functions[function];
  else
    return "INVALID_FUNCTION";
}

const char *
Urb::StatusToString (ULONG status)
{
  switch (status)
  {
    case USBD_STATUS_SUCCESS: return "USBD_STATUS_SUCCESS";
    case USBD_STATUS_PENDING: return "USBD_STATUS_PENDING";
    case USBD_STATUS_CRC: return "USBD_STATUS_CRC";
    case USBD_STATUS_BTSTUFF: return "USBD_STATUS_BTSTUFF";
    case USBD_STATUS_DATA_TOGGLE_MISMATCH: return "USBD_STATUS_DATA_TOGGLE_MISMATCH";
    case USBD_STATUS_STALL_PID: return "USBD_STATUS_STALL_PID";
    case USBD_STATUS_DEV_NOT_RESPONDING: return "USBD_STATUS_DEV_NOT_RESPONDING";
    case USBD_STATUS_PID_CHECK_FAILURE: return "USBD_STATUS_PID_CHECK_FAILURE";
    case USBD_STATUS_UNEXPECTED_PID: return "USBD_STATUS_UNEXPECTED_PID";
    case USBD_STATUS_DATA_OVERRUN: return "USBD_STATUS_DATA_OVERRUN";
    case USBD_STATUS_DATA_UNDERRUN: return "USBD_STATUS_DATA_UNDERRUN";
    case USBD_STATUS_RESERVED1: return "USBD_STATUS_RESERVED1";
    case USBD_STATUS_RESERVED2: return "USBD_STATUS_RESERVED2";
    case USBD_STATUS_BUFFER_OVERRUN: return "USBD_STATUS_BUFFER_OVERRUN";
    case USBD_STATUS_BUFFER_UNDERRUN: return "USBD_STATUS_BUFFER_UNDERRUN";
    case USBD_STATUS_NOT_ACCESSED: return "USBD_STATUS_NOT_ACCESSED";
    case USBD_STATUS_FIFO: return "USBD_STATUS_FIFO";
    case USBD_STATUS_XACT_ERROR: return "USBD_STATUS_XACT_ERROR";
    case USBD_STATUS_BABBLE_DETECTED: return "USBD_STATUS_BABBLE_DETECTED";
    case USBD_STATUS_DATA_BUFFER_ERROR: return "USBD_STATUS_DATA_BUFFER_ERROR";
    case USBD_STATUS_ENDPOINT_HALTED: return "USBD_STATUS_ENDPOINT_HALTED";
    case USBD_STATUS_INVALID_URB_FUNCTION: return "USBD_STATUS_INVALID_URB_FUNCTION";
    case USBD_STATUS_INVALID_PARAMETER: return "USBD_STATUS_INVALID_PARAMETER";
    case USBD_STATUS_ERROR_BUSY: return "USBD_STATUS_ERROR_BUSY";
    case USBD_STATUS_INVALID_PIPE_HANDLE: return "USBD_STATUS_INVALID_PIPE_HANDLE";
    case USBD_STATUS_NO_BANDWIDTH: return "USBD_STATUS_NO_BANDWIDTH";
    case USBD_STATUS_INTERNAL_HC_ERROR: return "USBD_STATUS_INTERNAL_HC_ERROR";
    case USBD_STATUS_ERROR_SHORT_TRANSFER: return "USBD_STATUS_ERROR_SHORT_TRANSFER";
    case USBD_STATUS_BAD_START_FRAME: return "USBD_STATUS_BAD_START_FRAME";
    case USBD_STATUS_ISOCH_REQUEST_FAILED: return "USBD_STATUS_ISOCH_REQUEST_FAILED";
    case USBD_STATUS_FRAME_CONTROL_OWNED: return "USBD_STATUS_FRAME_CONTROL_OWNED";
    case USBD_STATUS_FRAME_CONTROL_NOT_OWNED: return "USBD_STATUS_FRAME_CONTROL_NOT_OWNED";
    case USBD_STATUS_NOT_SUPPORTED: return "USBD_STATUS_NOT_SUPPORTED";
    case USBD_STATUS_INAVLID_CONFIGURATION_DESCRIPTOR: return "USBD_STATUS_INAVLID_CONFIGURATION_DESCRIPTOR";
    case USBD_STATUS_INSUFFICIENT_RESOURCES: return "USBD_STATUS_INSUFFICIENT_RESOURCES";
    case USBD_STATUS_SET_CONFIG_FAILED: return "USBD_STATUS_SET_CONFIG_FAILED";
    case USBD_STATUS_BUFFER_TOO_SMALL: return "USBD_STATUS_BUFFER_TOO_SMALL";
    case USBD_STATUS_INTERFACE_NOT_FOUND: return "USBD_STATUS_INTERFACE_NOT_FOUND";
    case USBD_STATUS_INAVLID_PIPE_FLAGS: return "USBD_STATUS_INAVLID_PIPE_FLAGS";
    case USBD_STATUS_TIMEOUT: return "USBD_STATUS_TIMEOUT";
    case USBD_STATUS_DEVICE_GONE: return "USBD_STATUS_DEVICE_GONE";
    case USBD_STATUS_STATUS_NOT_MAPPED: return "USBD_STATUS_STATUS_NOT_MAPPED";
    case USBD_STATUS_HUB_INTERNAL_ERROR: return "USBD_STATUS_HUB_INTERNAL_ERROR";
    case USBD_STATUS_CANCELED: return "USBD_STATUS_CANCELED";
    case USBD_STATUS_ISO_NOT_ACCESSED_BY_HW: return "USBD_STATUS_ISO_NOT_ACCESSED_BY_HW";
    case USBD_STATUS_ISO_TD_ERROR: return "USBD_STATUS_ISO_TD_ERROR";
    case USBD_STATUS_ISO_NA_LATE_USBPORT: return "USBD_STATUS_ISO_NA_LATE_USBPORT";
    case USBD_STATUS_ISO_NOT_ACCESSED_LATE: return "USBD_STATUS_ISO_NOT_ACCESSED_LATE";
    case USBD_STATUS_BAD_DESCRIPTOR: return "USBD_STATUS_BAD_DESCRIPTOR";
    case USBD_STATUS_BAD_DESCRIPTOR_BLEN: return "USBD_STATUS_BAD_DESCRIPTOR_BLEN";
    case USBD_STATUS_BAD_DESCRIPTOR_TYPE: return "USBD_STATUS_BAD_DESCRIPTOR_TYPE";
    case USBD_STATUS_BAD_INTERFACE_DESCRIPTOR: return "USBD_STATUS_BAD_INTERFACE_DESCRIPTOR";
    case USBD_STATUS_BAD_ENDPOINT_DESCRIPTOR: return "USBD_STATUS_BAD_ENDPOINT_DESCRIPTOR";
    case USBD_STATUS_BAD_INTERFACE_ASSOC_DESCRIPTOR: return "USBD_STATUS_BAD_INTERFACE_ASSOC_DESCRIPTOR";
    case USBD_STATUS_BAD_CONFIG_DESC_LENGTH: return "USBD_STATUS_BAD_CONFIG_DESC_LENGTH";
    case USBD_STATUS_BAD_NUMBER_OF_INTERFACES: return "USBD_STATUS_BAD_NUMBER_OF_INTERFACES";
    case USBD_STATUS_BAD_NUMBER_OF_ENDPOINTS: return "USBD_STATUS_BAD_NUMBER_OF_ENDPOINTS";
    case USBD_STATUS_BAD_ENDPOINT_ADDRESS: return "USBD_STATUS_BAD_ENDPOINT_ADDRESS";
    default: return "USBD_STATUS_UNKNOWN";
  }
}

static const char * descriptorTypes[] = {
  "USB_DEVICE_DESCRIPTOR_TYPE", // 0x01
  "USB_CONFIGURATION_DESCRIPTOR_TYPE",
  "USB_STRING_DESCRIPTOR_TYPE",
  "USB_INTERFACE_DESCRIPTOR_TYPE",
  "USB_ENDPOINT_DESCRIPTOR_TYPE",
  "USB_RESERVED_DESCRIPTOR_TYPE",
  "USB_CONFIG_POWER_DESCRIPTOR_TYPE",
  "USB_INTERFACE_POWER_DESCRIPTOR_TYPE",  // 0x08
};

const char *
Urb::DescriptorTypeToString (UCHAR descriptorType)
{
  if (descriptorType >= 1 && descriptorType <= 8)
    return descriptorTypes[descriptorType - 1];
  else
    return "INVALID_DESCRIPTOR_TYPE";
}

const char *
Urb::PipeTypeToString (USBD_PIPE_TYPE type)
{
  switch (type)
  {
    case UsbdPipeTypeControl:     return "UsbdPipeTypeControl";
    case UsbdPipeTypeIsochronous: return "UsbdPipeTypeIsochronous";
    case UsbdPipeTypeBulk:        return "UsbdPipeTypeBulk";
    case UsbdPipeTypeInterrupt:   return "UsbdPipeTypeInterrupt";
    default:                      return "UsbdPipeTypeUnknown";
  }
}

} // namespace oSpy
