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

#ifndef EVENT_H
#define EVENT_H

#include <wdm.h>

namespace oSpy {

class Node
{
public:
  void Initialize ();

  int GetFieldCount () const;
  int GetChildCount () const;

  void AppendChild (Node * node);

public:
  char * m_name;

  int m_fieldCapacity;
  char ** m_fieldKeys;
  char ** m_fieldValues;

  bool m_contentIsRaw;
  int m_contentSize;
  UCHAR * m_content;

  int m_childCapacity;
  Node ** m_children;
};

typedef struct
{
  int offset;
  int size;
  UCHAR * storage;
} StorageSlot;

#define EVENT_NUM_DYNAMIC_SLOTS 2

class Event : public Node
{
public:
  void Initialize (ULONG id, LARGE_INTEGER timestamp, const char * eventType, int childCapacity);
  void Destroy ();

  void AddFieldToNode (Node * node, const char * key, const char * value);
  void AddFieldToNodePrintf (Node * node, const char * key, const char * value, ...);

  Node * CreateElement (const char * name, int fieldCapacity=0, int childCapacity=0);
  Node * CreateTextNode (const char * name, int fieldCapacity, const char * content, ...);
  Node * CreateDataNode (const char * name, int fieldCapacity, const void * data, int dataSize);

  void * m_userData;

private:
  void * ReserveStorage (int size);
  char * CreateString (const char * str);

  void CreateFieldStorage (Node * node, int fieldCapacity);

  void CreateChildStorage (Node * node, int childCapacity);

  int m_offset;
  UCHAR m_storage[3996]; // PAGE_SIZE minus some headroom for base class and m_dynamicSlots

  StorageSlot m_dynamicSlots[EVENT_NUM_DYNAMIC_SLOTS];
};

} // namespace oSpy

#endif // EVENT_H
