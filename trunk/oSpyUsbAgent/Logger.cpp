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

  bool done = false;

  do
  {
    status = KeWaitForSingleObject (&m_stopEvent, Executive, KernelMode,
      FALSE, &timeout);
    done = (status == STATUS_SUCCESS);

    ProcessItems ();
  }
  while (!done);

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
Logger::ProcessItems ()
{
  SLIST_ENTRY * listEntry;

  while ((listEntry =
    ExInterlockedPopEntrySList (&m_items, &m_itemsLock)) != NULL)
  {
    LogEntry * entry = reinterpret_cast <LogEntry *> (listEntry);

    if (m_capture != NULL)
    {
      InterlockedIncrement (reinterpret_cast<volatile LONG *> (&m_capture->LogCount));
      InterlockedExchangeAdd (reinterpret_cast<volatile LONG *> (&m_capture->LogSize), sizeof (LogEntry));
    }

    WriteNode (&entry->event);

    entry->event.Destroy ();

    ExFreePool (entry);
  }
}

Event *
Logger::NewEvent (const char * eventType,
                  int childCapacity)
{
  LogEntry * logEntry = static_cast <LogEntry *> (
    ExAllocatePool (NonPagedPool, sizeof (LogEntry)));
  if (logEntry == NULL)
  {
    KdPrint (("ExAllocatePool failed"));
    return NULL;
  }

  ULONG id = InterlockedIncrement (reinterpret_cast<volatile LONG *> (&m_index));
  LARGE_INTEGER timestamp;
  KeQuerySystemTime (&timestamp);

  Event * ev = &logEntry->event;
  ev->Initialize (id, timestamp, eventType, childCapacity);

  return ev;
}

void
Logger::SubmitEvent (Event * ev)
{
  LogEntry * logEntry = reinterpret_cast <LogEntry *> (
    reinterpret_cast <UCHAR *> (ev) - sizeof (SLIST_ENTRY));

  ExInterlockedPushEntrySList (&m_items, &logEntry->entry, &m_itemsLock);
}

void
Logger::WriteNode (const Node * node)
{
  Write (node->m_name);

  int fieldCount = node->GetFieldCount ();
  Write (fieldCount);
  for (int i = 0; i < fieldCount; i++)
  {
    Write (node->m_fieldKeys[i]);
    Write (node->m_fieldValues[i]);
  }

  Write (node->m_contentIsRaw);
  Write (node->m_contentSize);
  WriteRaw (node->m_content, node->m_contentSize);

  int childCount = node->GetChildCount ();
  Write (childCount);
  for (int i = 0; i < childCount; i++)
  {
    WriteNode (node->m_children[i]);
  }
}

void
Logger::WriteRaw (const void * data, size_t dataSize)
{
  if (m_fileHandle == NULL || data == NULL || dataSize == 0)
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
Logger::Write (const char * str)
{
  if (str == NULL)
  {
    Write (static_cast <ULONG> (0));
    return;
  }

  size_t length = strlen (str);
  Write (static_cast <ULONG> (length));
  WriteRaw (str, length);
}
