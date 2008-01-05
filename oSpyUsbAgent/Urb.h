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

#ifndef URB_H
#define URB_H

#include <wdm.h>

#pragma warning(push)
#pragma warning(disable:4200)
#include <usbdi.h>
#pragma warning(pop)

namespace oSpy {

class Event;
class Node;

typedef void (*UrbFunctionParser) (const URB * urb, Event * ev, Node * parentNode, bool onEntry);

class Urb
{
public:
  static void AppendToNode (const URB * urb, Event * ev, Node * parentNode, bool onEntry);

private:
  static const UrbFunctionParser functionParsers[];

  static void ParseSelectConfiguration (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseSelectInterface (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseAbortPipe (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseGetCurrentFrameNumber (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseControlTransfer (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseBulkOrInterruptTransfer (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseIsochTransfer (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseGetDescriptorFromDevice (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseClassInterface (const URB * urb, Event * ev, Node * parentNode, bool onEntry);
  static void ParseSyncResetPipeAndClearStall (const URB * urb, Event * ev, Node * parentNode, bool onEntry);

  static void AppendConfigDescriptorToNode (const USB_CONFIGURATION_DESCRIPTOR * desc, Event * ev, Node * parentNode);
  static void AppendInterfaceInfoToNode (const USBD_INTERFACE_INFORMATION * info, Event * ev, Node * parentNode);
  static void AppendPipeInfoToNode (const USBD_PIPE_INFORMATION * info, Event * ev, Node * parentNode);
  static void AppendTransferFlagsToNode (ULONG flags, Event * ev, Node * parentNode);
  static void AppendTransferBufferToNode (const void * transferBuffer, int transferBufferLength, MDL * transferBufferMDL, Event * ev, Node * parentNode);

  static const char * FunctionToString (USHORT function);
  static const char * StatusToString (ULONG status);
  static const char * DescriptorTypeToString (UCHAR descriptorType);
  static const char * PipeTypeToString (USBD_PIPE_TYPE type);
};

} // namespace oSpy

#endif // URB_H