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

namespace InterceptPP {

namespace Logging {

void
Logger::LogInfo(const char *format, ...)
{
}

void
Logger::LogWarning(const char *format, ...)
{
}

void
Logger::LogError(const char *format, ...)
{
}

Event *
NullLogger::NewEvent(const OString &eventType)
{
    return new Event(this, m_id++, eventType);
}

Node::Node(const OString &name)
    : m_name(name), m_contentIsRaw(false)
{
}

Node::~Node()
{
    ChildListConstIter iter, endIter = m_children.end();
    for (iter = m_children.begin(); iter != endIter; iter++)
    {
        delete *iter;
    }
}

void
Node::AddField(const OString &name, const OString &value)
{
    m_fields.push_back(pair<OString, OString>(name, value));
}

DataNode::DataNode(const OString &name)
    : Node(name)
{
    m_contentIsRaw = true;
}

void
DataNode::SetData(const OString &data)
{
    m_content = data;
}

void
DataNode::SetData(const void *buf, int size)
{
    m_content.resize(size);
    memcpy(const_cast<char *>(m_content.data()), buf, size);
}

Event::Event(Logger *logger, unsigned int id, const OString &eventType)
    : Element("Event"), m_logger(logger)
{
    OOStringStream ss;
    ss << id;
    AddField("Id", ss.str());

    AddField("Type", eventType);
}

} // namespace Logging

} // namespace InterceptPP