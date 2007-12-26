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

NTSTATUS
Logger::Initialize ()
{
  NTSTATUS status;

  UNICODE_STRING logfilePath;
  WCHAR buffer[256];
  logfilePath.Buffer = buffer;
  logfilePath.Length = 0;
  logfilePath.MaximumLength = sizeof (buffer);

  LARGE_INTEGER curTime;
  KeQuerySystemTime (&curTime);
  status = RtlUnicodeStringPrintf (&logfilePath,
    L"\\SystemRoot\\oSpyUsbAgent-%I64d.log", curTime.QuadPart);
  if (!NT_SUCCESS (status))
    return status;

  OBJECT_ATTRIBUTES attrs;
  InitializeObjectAttributes (&attrs, &logfilePath, 0, NULL, NULL);

  IO_STATUS_BLOCK ioStatus;
  status = ZwCreateFile (&m_fileHandle, GENERIC_WRITE, &attrs, &ioStatus,
    NULL, FILE_ATTRIBUTE_NORMAL, FILE_SHARE_READ, FILE_OVERWRITE_IF,
    FILE_SYNCHRONOUS_IO_NONALERT, NULL, 0);
  /*
  if (NT_SUCCESS (status))
  {
    unsigned char bom[2] = { 0xff, 0xfe };
    WriteRaw (bom, sizeof (bom));
  }
  */

  return status;
}

void
Logger::Shutdown ()
{
  if (m_fileHandle != NULL)
  {
    ZwClose (m_fileHandle);
    m_fileHandle = NULL;
  }
}

void
Logger::WriteRaw (void * data, size_t dataSize)
{
  if (m_fileHandle == NULL)
    return;

  NTSTATUS status;
  IO_STATUS_BLOCK io_status;
  status = ZwWriteFile (m_fileHandle, NULL, NULL, NULL, &io_status, data,
    static_cast <ULONG> (dataSize), 0, NULL);

  KdPrint (("ZwWriteFile returned 0x%08x", status));
}

void
Logger::WriteLine (const WCHAR * format, ...)
{
  va_list argList;
  va_start (argList, format);

  WCHAR buf[2048];
  if (!NT_SUCCESS (RtlStringCbVPrintfW (buf, sizeof (buf), format, argList)))
    return;

  size_t rawLength;
  if (!NT_SUCCESS (RtlStringCbLengthW (buf, sizeof (buf), &rawLength)))
    return;

  size_t crlfAndTermSize = (2 + 1) * sizeof (WCHAR);
  size_t crlfOffset = min (rawLength, sizeof (buf) - crlfAndTermSize);
  size_t crlfPos = crlfOffset / sizeof (WCHAR);
  RtlStringCbCopyW (buf + crlfPos, crlfAndTermSize, L"\r\n");

  WriteRaw (buf, crlfOffset + crlfAndTermSize - sizeof (WCHAR));
}

#if 0
static NTSTATUS
logfile_write_line (const char * format, ...)
{
  va_list va;


}
#endif
