//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

#include "Logging.h"
#include "Util.h"
#include <strsafe.h>

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

namespace Logging {

void
Logger::LogDebug(const char *format, ...)
{
    va_list args;
    va_start(args, format);

    LogMessage("Debug", format, args);

    va_end(args);
}

void
Logger::LogInfo(const char *format, ...)
{
    va_list args;
    va_start(args, format);

    LogMessage("Info", format, args);

    va_end(args);
}

void
Logger::LogWarning(const char *format, ...)
{
    va_list args;
    va_start(args, format);

    LogMessage("Warning", format, args);

    va_end(args);
}

void
Logger::LogError(const char *format, ...)
{
    va_list args;
    va_start(args, format);

    LogMessage("Error", format, args);

    va_end(args);
}

#define LOG_BUFFER_SIZE 2048

void
Logger::LogMessage(const char *type, const char *format, va_list args)
{
    char buf[LOG_BUFFER_SIZE];
    StringCbVPrintfA(buf, sizeof(buf), format, args);

    Event *ev = NewEvent(type);
    ev->AppendChild(new TextNode("message", buf));
    ev->Submit();
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

void
Node::AddField(const OString &name, int value)
{
    OOStringStream ss;
    ss << value;
    AddField(name, ss.str());
}

void
Node::AddField(const OString &name, unsigned int value)
{
    OOStringStream ss;
    ss << value;
    AddField(name, ss.str());
}

void
Node::AddField(const OString &name, unsigned long value)
{
    OOStringStream ss;
    ss << value;
    AddField(name, ss.str());
}

void
Node::AddField(const OString &name, unsigned long long value)
{
    OOStringStream ss;
    ss << value;
    AddField(name, ss.str());
}

TextNode::TextNode(const OString &name, const OString &text)
    : Node(name)
{
    m_content = text;
}

TextNode::TextNode(const OString &name, const char *text)
    : Node(name)
{
    m_content = text;
}

TextNode::TextNode(const OString &name, void *pointer)
    : Node(name)
{
    OOStringStream ss;
    ss << "0x" << hex << reinterpret_cast<DWORD>(pointer);
    m_content = ss.str();
}

TextNode::TextNode(const OString &name, int value)
    : Node(name)
{
    OOStringStream ss;
    ss << value;
    m_content = ss.str();
}

TextNode::TextNode(const OString &name, DWORD value)
    : Node(name)
{
    OOStringStream ss;
    ss << value;
    m_content = ss.str();
}

TextNode::TextNode(const OString &name, __int64 value)
    : Node(name)
{
    OOStringStream ss;
    ss << value;
    m_content = ss.str();
}

TextNode::TextNode(const OString &name, float value)
    : Node(name)
{
    OOStringStream ss;
    ss << value;
    m_content = ss.str();
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
    : Element("event"), m_logger(logger), m_id(id)
{
    AddField("id", id);

    AddField("type", eventType);

    FILETIME ft;
    GetSystemTimeAsFileTime(&ft);
    unsigned long long stamp = (((unsigned long long) ft.dwHighDateTime) << 32) | ((unsigned long long) ft.dwLowDateTime);
    AddField("timestamp", stamp);

    AddField("processName", Util::Instance()->GetProcessName());
    AddField("processId", GetCurrentProcessId());
    AddField("threadId", GetCurrentThreadId());
}

} // namespace Logging

} // namespace InterceptPP
