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

#include "Event.h"

#include <ntstrsafe.h>

void
Node::Initialize ()
{
  m_name = NULL;

  m_numFieldsMax = 0;
  m_fieldKeys = NULL;
  m_fieldValues = NULL;

  m_contentIsRaw = false;
  m_contentSize = 0;
  m_content = NULL;

  m_numChildrenMax = 0;
  m_children = NULL;
}

int
Node::GetFieldCount () const
{
  int count = 0;

  for (int i = 0; i < m_numFieldsMax; i++)
  {
    if (m_fieldKeys[i] != NULL)
      count ++;
    else
      break;
  }

  return count;
}

int
Node::GetChildCount () const
{
  int count = 0;

  for (int i = 0; i < m_numChildrenMax; i++)
  {
    if (m_children[i] != NULL)
      count ++;
    else
      break;
  }

  return count;
}

void
Node::AppendChild (Node * node)
{
  int slotIndex = GetChildCount ();
  if (slotIndex == m_numChildrenMax)
  {
    KdPrint (("Not enough elements reserved for AppendChild"));
    return;
  }

  m_children[slotIndex] = node;
}

void
Event::Initialize (ULONG id,
                   LARGE_INTEGER timestamp,
                   const char * eventType,
                   int numChildrenMax)
{
  Node::Initialize ();

  m_offset = 0;

  m_name = CreateString ("Event");

  CreateFieldStorage (this, 6);
  AppendFieldPrintf (this, "id", "%lu", id);
  AppendFieldPrintf (this, "timestamp", "%lld", timestamp.QuadPart);
  AppendField (this, "type", eventType);
  AppendField (this, "processName", "oSpyUsbAgent.sys");
  AppendField (this, "processId", "0");
  AppendField (this, "threadId", "0");

  CreateChildStorage (this, numChildrenMax);
}

Node *
Event::CreateTextNode (const char * name, const char * content, ...)
{
  Node * node = static_cast <Node *> (ReserveStorage (sizeof (Node)));
  node->Initialize ();

  node->m_name = CreateString (name);

  va_list argList;
  va_start (argList, content);

  char buf[1024];
  NTSTATUS status;
  status = RtlStringCbVPrintfA (buf, sizeof (buf), content, argList);
  if (NT_SUCCESS (status))
  {
    node->m_content = reinterpret_cast <UCHAR *> (CreateString (buf));
    node->m_contentSize = static_cast <int> (strlen (buf));
  }
  else
  {
    KdPrint (("RtlStringCbVPrintfA failed"));
  }

  return node;
}

void *
Event::ReserveStorage (int size)
{
  if (static_cast <int> (sizeof (m_storage)) - m_offset < size)
  {
    KdPrint (("ReserveStorageRaw failed, should never happen"));
    return ExAllocatePool (NonPagedPool, size);
  }

  void * result = m_storage + m_offset;
  m_offset += size;
  return result;
}

char *
Event::CreateString (const char * str)
{
  int storageNeeded = static_cast <ULONG> (strlen (str)) + 1;

  char * result = static_cast <char *> (ReserveStorage (storageNeeded));
  RtlStringCbCopyA (result, storageNeeded, str);
  return result;
}

void
Event::CreateFieldStorage (Node * node, int numFieldsMax)
{
  node->m_numFieldsMax = numFieldsMax;

  int size = sizeof (char *) * numFieldsMax;

  node->m_fieldKeys = static_cast <char **> (
    ReserveStorage (size));
  memset (node->m_fieldKeys, 0, size);

  node->m_fieldValues = static_cast <char **> (
    ReserveStorage (size));
  memset (node->m_fieldValues, 0, size);
}

void
Event::AppendField (Node * node,
                    const char * key,
                    const char * value)
{
  int slotIndex = node->GetFieldCount ();
  if (slotIndex == node->m_numFieldsMax)
  {
    KdPrint (("Not enough fields reserved for AppendFieldRaw"));
    return;
  }

  node->m_fieldKeys[slotIndex] = CreateString (key);
  node->m_fieldValues[slotIndex] = CreateString (value);
}

void
Event::AppendFieldPrintf (Node * node,
                          const char * key,
                          const char * valueFormat, ...)
{
  va_list argList;
  va_start (argList, valueFormat);

  char buf[1024];
  NTSTATUS status;
  status = RtlStringCbVPrintfA (buf, sizeof (buf), valueFormat, argList);
  if (!NT_SUCCESS (status)) return;

  AppendField (node, key, buf);
}

void
Event::CreateChildStorage (Node * node, int numChildrenMax)
{
  node->m_numChildrenMax = numChildrenMax;

  int size = sizeof (Node *) * numChildrenMax;

  node->m_children = static_cast <Node **> (ReserveStorage (size));
  memset (node->m_children, 0, size);
}
