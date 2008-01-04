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
  NULL,                             // URB_FUNCTION_SELECT_INTERFACE
  NULL,                             // URB_FUNCTION_ABORT_PIPE
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
  Node * urbNode = ev->CreateElement ("urb", 1, 9);
  ev->AddFieldToNode (urbNode, "direction", (onEntry) ? "in" : "out");
  parentNode->AppendChild (urbNode);

  Node * node;
  node = ev->CreateTextNode ("type", 0, "%s",
    FunctionToString (urb->UrbHeader.Function));
  urbNode->AppendChild (node);

  USHORT function = urb->UrbHeader.Function;
  if (function <= URB_FUNCTION_MAX && functionParsers[function] != NULL)
  {
    functionParsers[function] (urb, ev, urbNode, onEntry);
  }
  else
  {
    node = ev->CreateTextNode ("FIXME", 0, "This URB is not yet handled.");
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

  node = ev->CreateTextNode ("startFrame", 0, "%d", xfer->StartFrame);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("numberOfPackets", 0, "%d",
    xfer->NumberOfPackets);
  parentNode->AppendChild (node);

  node = ev->CreateTextNode ("errorCount", 0, "%d", xfer->ErrorCount);
  parentNode->AppendChild (node);

  /*
  for (ULONG i = 0; i < xfer->NumberOfPackets; i++)
  {
    const USBD_ISO_PACKET_DESCRIPTOR * pkt = &xfer->IsoPacket[i];
  }
  */
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
Urb::AppendConfigDescriptorToNode (USB_CONFIGURATION_DESCRIPTOR * desc,
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

const char *
Urb::FunctionToString (USHORT function)
{
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

  if (function <= URB_FUNCTION_MAX)
    return functions[function];
  else
    return "INVALID_FUNCTION";
}

const char *
Urb::DescriptorTypeToString (UCHAR descriptorType)
{
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

  if (descriptorType >= 1 && descriptorType <= 8)
    return descriptorTypes[descriptorType - 1];
  else
    return "INVALID_DESCRIPTOR_TYPE";
}

} // namespace oSpy
