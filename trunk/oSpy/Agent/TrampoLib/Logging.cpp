//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#include "stdafx.h"
#include "Logging.h"

namespace TrampoLib {

namespace Logging {

Event *
NullLogger::NewEvent(const OString &eventType)
{
    return new Event(this, m_id++, eventType);
}

void
Node::AddField(const OString &name, const OString &value)
{
    m_fields[name] = value;
}

Node *
Node::AppendChild(const OString &name)
{
    Node *child = new Node(name);
    AppendChild(child);
    return child;
}

void
Node::AppendChild(Node *node)
{
    m_children.push_back(node);
}

Event::Event(Logger *logger, unsigned int id, const OString &eventType)
    : Node("Event"), m_logger(logger), m_id(id)
{
    AddField("Type", eventType);
}

} // namespace Logging

} // namespace TrampoLib