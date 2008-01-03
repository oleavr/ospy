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

class Urb
{
public:
  static void AppendToNode (const URB * urb, Event * ev, Node * parentNode, bool onEntry);

private:
  static void AppendTransferBufferToNode (const void * transferBuffer, int transferBufferLength, MDL * transferBufferMDL, Event * ev, Node * parentNode);
};

} // namespace oSpy

#endif // URB_H