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

void
Urb::AppendToNode (const URB * urb,
                   Event * ev,
                   Node * parentNode,
                   bool onEntry)
{
  Node * urbNode = ev->CreateElement ("urb", 1, 4);
  ev->AddFieldToNode (urbNode, "direction", (onEntry) ? "in" : "out");
  parentNode->AppendChild (urbNode);

  Node * node;
  if (urb->UrbHeader.Function <= URB_FUNCTION_MAX)
    node = ev->CreateTextNode ("type", 0, "%s", urbFunctions[urb->UrbHeader.Function]);
  else
    node = ev->CreateTextNode ("type", 0, "%d", urb->UrbHeader.Function);
  urbNode->AppendChild (node);

  if (urb->UrbHeader.Function == URB_FUNCTION_GET_DESCRIPTOR_FROM_DEVICE)
  {
    const struct _URB_CONTROL_DESCRIPTOR_REQUEST * req =
      reinterpret_cast <const struct _URB_CONTROL_DESCRIPTOR_REQUEST *>
      (urb);
  }
  else if (urb->UrbHeader.Function == URB_FUNCTION_CONTROL_TRANSFER)
  {
    const struct _URB_CONTROL_TRANSFER * xfer =
      reinterpret_cast <const struct _URB_CONTROL_TRANSFER *> (urb);

    node = ev->CreateTextNode ("direction", 0,
      (xfer->TransferFlags & USBD_TRANSFER_DIRECTION_IN) ? "in" : "out");
    urbNode->AppendChild (node);

    node = ev->CreateTextNode ("shortTransferOk", 0,
      (xfer->TransferFlags & USBD_SHORT_TRANSFER_OK) ? "true" : "false");
    urbNode->AppendChild (node);

    AppendTransferBufferToNode (xfer->TransferBuffer,
      xfer->TransferBufferLength, xfer->TransferBufferMDL, ev, urbNode);
  }
  else if (urb->UrbHeader.Function == URB_FUNCTION_CLASS_INTERFACE)
  {
    const struct _URB_CONTROL_VENDOR_OR_CLASS_REQUEST * req =
      reinterpret_cast <const struct _URB_CONTROL_VENDOR_OR_CLASS_REQUEST *> (urb);

    node = ev->CreateTextNode ("direction", 0,
      (req->TransferFlags & USBD_TRANSFER_DIRECTION_IN) ? "in" : "out");
    urbNode->AppendChild (node);

    node = ev->CreateTextNode ("shortTransferOk", 0,
      (req->TransferFlags & USBD_SHORT_TRANSFER_OK) ? "true" : "false");
    urbNode->AppendChild (node);

    AppendTransferBufferToNode (req->TransferBuffer,
      req->TransferBufferLength, req->TransferBufferMDL, ev, urbNode);
  }
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

} // namespace oSpy