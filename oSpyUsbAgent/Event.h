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

class Node
{
public:
  void Initialize ();

  int GetFieldCount () const;
  int GetChildCount () const;

  void AppendChild (Node * node);

public:
  char * m_name;

  int m_numFieldsMax;
  char ** m_fieldKeys;
  char ** m_fieldValues;

  bool m_contentIsRaw;
  int m_contentSize;
  UCHAR * m_content;

  int m_numChildrenMax;
  Node ** m_children;
};

class Event : public Node
{
public:
  void Initialize (ULONG id, LARGE_INTEGER timestamp, const char * eventType, int numChildrenMax);

  Node * CreateTextNode (const char * name, const char * content, ...);

private:
  void * ReserveStorage (int size);
  char * CreateString (const char * str);

  void CreateFieldStorage (Node * node, int numFieldsMax);
  void AppendField (Node * node, const char * key, const char * value);
  void AppendFieldPrintf (Node * node, const char * key, const char * value, ...);

  void CreateChildStorage (Node * node, int numChildrenMax);

  int m_offset;
  UCHAR m_storage[4000]; // PAGE_SIZE minus some headroom
};

#endif // EVENT_H
