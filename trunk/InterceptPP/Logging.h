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

#pragma once

namespace InterceptPP {

namespace Logging {

class Event;

class Logger : public BaseObject
{
public:
    virtual ~Logger() {}

    virtual Event *NewEvent(const OString &eventType) = 0;
    virtual void SubmitEvent(Event *ev) = 0;

    void LogDebug(const char *format, ...);
    void LogInfo(const char *format, ...);
    void LogWarning(const char *format, ...);
    void LogError(const char *format, ...);

protected:
    void LogMessage(const char *type, const char *format, va_list args);
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
    void AddField(const OString &name, int value);
    void AddField(const OString &name, unsigned int value);
    void AddField(const OString &name, unsigned long value);
    void AddField(const OString &name, unsigned long long value);

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
    TextNode(const OString &name, const OString &text="");
    TextNode(const OString &name, const char *text);
    TextNode(const OString &name, void *pointer);
    TextNode(const OString &name, int value);
    TextNode(const OString &name, DWORD value);
    TextNode(const OString &name, __int64 value);
    TextNode(const OString &name, float value);

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

    unsigned int GetId() const { return m_id; }

    void Submit() { m_logger->SubmitEvent(this); }

protected:
    Logger *m_logger;
    unsigned int m_id;
};

} // namespace Logging

} // namespace InterceptPP
