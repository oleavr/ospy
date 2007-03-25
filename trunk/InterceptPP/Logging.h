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

#pragma once

namespace InterceptPP {

namespace Logging {

class Event;

class Logger : public BaseObject
{
public:
    virtual Event *NewEvent(const OString &eventType) = 0;
    virtual void SubmitEvent(Event *ev) = 0;

    void LogInfo(const char *format, ...);
    void LogWarning(const char *format, ...);
    void LogError(const char *format, ...);

protected:
    void LogMessage(const char *type, const char *format, va_list args);
};

class NullLogger : public Logger
{
public:
    NullLogger()
        : m_id(0)
    {}

    virtual Event *NewEvent(const OString &eventType);
    virtual void SubmitEvent(Event *ev) {}

protected:
    unsigned int m_id;
};

class Node : public BaseObject
{
public:
    typedef OList<pair<OString, OString>>::Type FieldList;
    typedef FieldList::const_iterator FieldListConstIter;

    typedef OList<Node *>::Type ChildList;
    typedef ChildList::const_iterator ChildListConstIter;

    Node(const OString &name);
    ~Node();

    const OString &GetName() const { return m_name; }

    bool GetContentIsRaw() const { return m_contentIsRaw; }
    const OString &GetContent() const { return m_content; }

	unsigned int GetFieldCount() const { return static_cast<unsigned int>(m_fields.size()); }
    FieldListConstIter FieldsIterBegin() const { return m_fields.begin(); }
    FieldListConstIter FieldsIterEnd() const { return m_fields.end(); }
    void AddField(const OString &name, const OString &value);

	unsigned int GetChildCount() const { return static_cast<unsigned int>(m_children.size()); }
    ChildListConstIter ChildrenIterBegin() const { return m_children.begin(); }
    ChildListConstIter ChildrenIterEnd() const { return m_children.end(); }

protected:
    OString m_name;
    bool m_contentIsRaw;
    OString m_content;
    FieldList m_fields;
    ChildList m_children;
};

class Element : public Node
{
public:
    Element(const OString &name)
        : Node(name)
    {}

    void AppendChild(Node *node) { m_children.push_back(node); }
};

class TextNode : public Node
{
public:
    TextNode(const OString &name, const OString &text="")
        : Node(name)
    {
        m_content = text;
    }

    void SetText(const OString &text) { m_content = text; }
};

class DataNode : public Node
{
public:
    DataNode(const OString &name);

    void SetData(const OString &data);
    void SetData(const void *buf, int size);
};

class Event : public Element
{
public:
    Event(Logger *logger, unsigned int id, const OString &eventType);

    void Submit() { m_logger->SubmitEvent(this); }

protected:
    Logger *m_logger;
};

} // namespace Logging

} // namespace InterceptPP