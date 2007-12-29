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

#include <ntstrsafe.h>

HANDLE Logger::m_captureSection = NULL;
Capture * Logger::m_capture = NULL;
volatile ULONG Logger::m_index = 0;

void
Logger::Initialize ()
{
  m_captureSection = NULL;
  m_capture = NULL;

  UNICODE_STRING objName;
  RtlInitUnicodeString (&objName, L"\\BaseNamedObjects\\oSpyCapture");

  OBJECT_ATTRIBUTES attrs;
  InitializeObjectAttributes (&attrs, &objName, 0, NULL, NULL);

  NTSTATUS status;
  status = ZwOpenSection (&m_captureSection, SECTION_ALL_ACCESS, &attrs);
  if (NT_SUCCESS (status))
  {
    SIZE_T size = sizeof (Capture);

    status = ZwMapViewOfSection (m_captureSection, NtCurrentProcess (),
      reinterpret_cast <void **> (&m_capture), 0L, sizeof (Capture),
      NULL, &size, ViewUnmap, 0, PAGE_READWRITE | PAGE_NOCACHE);

    if (!NT_SUCCESS (status))
    {
      KdPrint (("ZwMapViewOfSection failed: 0x%08x", status));
    }
  }
  else
  {
    KdPrint (("ZwOpenSection failed: 0x%08x", status));
  }

  m_index = 0;
}

void
Logger::Shutdown ()
{
  NTSTATUS status;

  if (m_capture != NULL)
  {
    status = ZwUnmapViewOfSection (NtCurrentProcess (), m_capture);
    if (!NT_SUCCESS (status))
      KdPrint (("ZwUnmapViewOfSection failed: 0x%08x", status));
  }

  if (m_captureSection != NULL)
  {
    status = ZwClose (m_captureSection);
    if (!NT_SUCCESS (status))
      KdPrint (("ZwClose failed: 0x%08x", status));
  }
}

NTSTATUS
Logger::Start (IO_REMOVE_LOCK * removeLock, const WCHAR * fnSuffix)
{
  NTSTATUS status = STATUS_SUCCESS;

  status = IoAcquireRemoveLock (removeLock, this);
  if (!NT_SUCCESS (status))
    return status;
  m_removeLock = removeLock;

  UNICODE_STRING logfilePath;
  WCHAR buffer[256];
  RtlInitEmptyUnicodeString (&logfilePath, buffer, sizeof (buffer));

  __try
  {
    if (m_capture != NULL)
      status = RtlUnicodeStringPrintf (&logfilePath,
        L"\\DosDevices\\%s\\oSpyUsbAgent-%s.log", m_capture->LogPath,
        fnSuffix);
    else
      status = RtlUnicodeStringPrintf (&logfilePath,
        L"\\SystemRoot\\oSpyUsbAgent-%s.log", fnSuffix);

    if (!NT_SUCCESS (status))
    {
      KdPrint (("RtlUnicodeStringPrintf failed: 0x%08x", status));
      return status;
    }

    KdPrint (("Logging to %S", buffer));

    m_fileHandle = NULL;

    KeInitializeEvent (&m_stopEvent, NotificationEvent, FALSE);
    m_logThread = NULL;

    ExInitializeSListHead (&m_items);
    KeInitializeSpinLock (&m_itemsLock);

    OBJECT_ATTRIBUTES attrs;
    InitializeObjectAttributes (&attrs, &logfilePath, 0, NULL, NULL);

    IO_STATUS_BLOCK ioStatus;
    status = ZwCreateFile (&m_fileHandle, GENERIC_WRITE, &attrs, &ioStatus,
      NULL, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ, FILE_OVERWRITE_IF,
      FILE_SYNCHRONOUS_IO_NONALERT, NULL, 0);
    if (!NT_SUCCESS (status))
    {
      KdPrint (("ZwCreateFile failed: 0x%08x", status));
      return status;
    }

    status = PsCreateSystemThread (&m_logThread, THREAD_ALL_ACCESS, NULL,
      NULL, NULL, LogThreadFuncWrapper, this);
    if (!NT_SUCCESS (status))
    {
      KdPrint (("PsCreateSystemThread failed: 0x%08x", status));
      return status;
    }
  }
  __finally
  {
    if (!NT_SUCCESS (status))
    {
      if (m_fileHandle != NULL)
      {
        ZwClose (m_fileHandle);
        m_fileHandle = NULL;
      }

      IoReleaseRemoveLock (m_removeLock, this);
      m_removeLock = NULL;
    }
  }

  return status;
}

void
Logger::Stop ()
{
  KeSetEvent (&m_stopEvent, IO_NO_INCREMENT, FALSE);

  if (m_logThread != NULL)
  {
    ZwClose (m_logThread);
    m_logThread = NULL;
  }
}

void
Logger::LogThreadFunc ()
{
  KdPrint (("Logger thread speaking"));

  NTSTATUS status;
  LARGE_INTEGER timeout;

  timeout.QuadPart = -1000000; // 100 ms

  do
  {
    status = KeWaitForSingleObject (&m_stopEvent, Executive, KernelMode,
      FALSE, &timeout);
    if (status == STATUS_SUCCESS)
      break;

    SLIST_ENTRY * listEntry;

    while ((listEntry =
      ExInterlockedPopEntrySList (&m_items, &m_itemsLock)) != NULL)
    {
      UrbLogEntry * entry = reinterpret_cast <UrbLogEntry *> (listEntry);

      if (m_capture != NULL)
      {
        InterlockedIncrement (reinterpret_cast<volatile LONG *> (&m_capture->LogCount));
        InterlockedExchangeAdd (reinterpret_cast<volatile LONG *> (&m_capture->LogSize), sizeof (URB));
      }

      WriteUrbEntry (entry);

      ExFreePool (entry);
    }
  }
  while (true);

  if (m_fileHandle != NULL)
  {
    ZwClose (m_fileHandle);
    m_fileHandle = NULL;
  }

  IoReleaseRemoveLock (m_removeLock, this);

  KdPrint (("Logger thread terminating"));

  PsTerminateSystemThread (STATUS_SUCCESS);
}

void
Logger::LogUrb (const URB * urb)
{
  UrbLogEntry * logEntry = static_cast <UrbLogEntry *> (
    ExAllocatePool (NonPagedPool, sizeof (UrbLogEntry)));

  if (logEntry == NULL)
  {
    KdPrint (("ExAllocatePool failed"));
    return;
  }

  logEntry->id = InterlockedIncrement (reinterpret_cast<volatile LONG *> (&m_index));
  KeQuerySystemTime (&logEntry->timestamp);
  logEntry->urb = *urb;

  ExInterlockedPushEntrySList (&m_items, &logEntry->entry, &m_itemsLock);
}

void
Logger::WriteUrbEntry (const UrbLogEntry * entry)
{
  // Name
  Write ("Event");

  // Fields
  Write (6);
  
  Write ("id");
  Write ("%lu", entry->id);

  Write ("type");
  Write ("IOCTL_INTERNAL_USB_SUBMIT_URB");

  Write ("timestamp");
  Write ("%lld", entry->timestamp.QuadPart);

  Write ("processName");
  Write ("ntoskrnl.dll");

  Write ("processId");
  Write ("0");

  Write ("threadId");
  Write ("0");

  // Content
  Write (1); // is raw
  Write (sizeof (URB));
  WriteRaw (&entry->urb, sizeof (URB));

  // Children
  Write (static_cast <ULONG> (0));
}

void
Logger::WriteRaw (const void * data, size_t dataSize)
{
  if (m_fileHandle == NULL)
    return;

  NTSTATUS status;
  IO_STATUS_BLOCK io_status;
  status = ZwWriteFile (m_fileHandle, NULL, NULL, NULL, &io_status,
    const_cast <void *> (data), static_cast <ULONG> (dataSize), 0, NULL);
  if (!NT_SUCCESS (status))
    KdPrint (("ZwWriteFile failed: 0x%08x", status));
}

void
Logger::Write (ULONG dw)
{
  WriteRaw (&dw, sizeof (dw));
}

void
Logger::Write (const char * format, ...)
{
  NTSTATUS status;

  va_list argList;
  va_start (argList, format);

  char buf[1024];
  status = RtlStringCbVPrintfA (buf, sizeof (buf), format, argList);
  if (!NT_SUCCESS (status)) return;

  size_t length;
  status = RtlStringCbLengthA (buf, sizeof (buf), &length);
  if (!NT_SUCCESS (status)) return;

  Write (static_cast <ULONG> (length));
  WriteRaw (buf, length);
}
