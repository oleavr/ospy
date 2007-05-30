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

TextNode::TextNode(const OString &name, DWORD value)
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