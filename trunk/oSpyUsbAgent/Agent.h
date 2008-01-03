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

#ifndef AGENT_H
#define AGENT_H

#include <wdm.h>

namespace oSpy {

class Agent
{
public:
  static void Initialize (DRIVER_OBJECT * driverObject);

private:
  // Handlers
  static void OnDriverUnload (DRIVER_OBJECT * driverObject);

  static NTSTATUS OnAddDevice (DRIVER_OBJECT * driverObject, DEVICE_OBJECT * physicalDeviceObject);

  static NTSTATUS OnAnyIrp (DEVICE_OBJECT * filterDeviceObject, IRP * irp);
  static NTSTATUS OnPowerIrp (DEVICE_OBJECT * filterDeviceObject, IRP * irp);
  static NTSTATUS OnPnpIrp (DEVICE_OBJECT * filterDeviceObject, IRP * irp);
  static NTSTATUS OnInternalIoctlIrp (DEVICE_OBJECT * filterDeviceObject, IRP * irp);

  static NTSTATUS OnUrbIoctlCompletion (DEVICE_OBJECT * filterDeviceObject, IRP * irp, void * context);

private:
  // Utility functions
  static void CanonicalizeFilename (WCHAR * s);
  static NTSTATUS CompleteRequest (IRP * irp, NTSTATUS status);
};

} // namespace oSpy

#endif // AGENT_H