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
#include "Marshallers.h"

namespace InterceptPP {

BaseMarshaller::BaseMarshaller(const OString &typeName)
	: m_typeName(typeName)
{
}

bool
BaseMarshaller::HasPropertyBinding(const OString &propName) const
{
    return (m_propBindings.find(propName) != m_propBindings.end());
}

const OString &
BaseMarshaller::GetPropertyBinding(const OString &propName) const
{
    PropBindingsMap::const_iterator iter = m_propBindings.find(propName);
    return iter->second;
}

void
BaseMarshaller::SetPropertyBindings(const char *firstPropName, ...)
{
    va_list args;
    va_start(args, firstPropName);

    const char *propName = firstPropName;
    while (propName != NULL)
    {
		const char *propValue = va_arg(args, const char *);

		m_propBindings[propName] = propValue;

        propName = va_arg(args, const char *);
    }

    va_end(args);
}

Logging::Node *
BaseMarshaller::ToNode(const void *start, bool deep, IPropertyProvider *propProv) const
{
    Logging::Element *el = new Logging::Element("Value");

    el->AddField("Type", m_typeName);
    el->AddField("Value", ToString(start, false, propProv));

    return el;
}

namespace Marshaller {

Factory *
Factory::Instance()
{
    static Factory *fact = NULL;

    if (fact == NULL)
        fact = new Factory();

    return fact;
}

#define REGISTER_MARSHALLER(m) m_marshallers[#m] = CreateMarshallerInstance<m>

Factory::Factory()
{
    REGISTER_MARSHALLER(Pointer);
    REGISTER_MARSHALLER(UInt16);
    REGISTER_MARSHALLER(UInt16BE);
    REGISTER_MARSHALLER(UInt32);
    REGISTER_MARSHALLER(UInt32BE);
    REGISTER_MARSHALLER(UInt32Ptr);
    REGISTER_MARSHALLER(ByteArray);
    REGISTER_MARSHALLER(ByteArrayPtr);
    REGISTER_MARSHALLER(AsciiString);
    REGISTER_MARSHALLER(AsciiStringPtr);
    REGISTER_MARSHALLER(UnicodeString);
    REGISTER_MARSHALLER(UnicodeStringPtr);
    REGISTER_MARSHALLER(Registry::KeyHandle);
    REGISTER_MARSHALLER(Winsock::Ipv4InAddr);
    REGISTER_MARSHALLER(Winsock::Ipv4Sockaddr);
    REGISTER_MARSHALLER(Winsock::Ipv4SockaddrPtr);
}

template<class T> BaseMarshaller *
Factory::CreateMarshallerInstance(const OString &name)
{
    return new T();
}

bool
Factory::HasMarshaller(const OString &name)
{
    MarshallerMap::iterator iter = m_marshallers.find(name);
    return (iter != m_marshallers.end());
}

void
Factory::RegisterMarshaller(const OString &name, CreateMarshallerFunc createFunc)
{
    m_marshallers[name] = createFunc;
}

void
Factory::UnregisterMarshaller(const OString &name)
{
    MarshallerMap::iterator iter = m_marshallers.find(name);
    if (iter != m_marshallers.end())
    {
        m_marshallers.erase(iter);
    }
}

BaseMarshaller *
Factory::CreateMarshaller(const OString &name)
{
    if (m_marshallers.find(name) != m_marshallers.end())
        return m_marshallers[name](name);
    else
        return NULL;
}

Pointer::Pointer(BaseMarshaller *type, const OString &ptrTypeName)
    : BaseMarshaller(ptrTypeName), m_type(type)
{}

Pointer::~Pointer()
{
    if (m_type != NULL)
        delete m_type;
}

BaseMarshaller *
Pointer::Clone() const
{
    BaseMarshaller *typeClone = NULL;
    if (m_type)
        typeClone = m_type->Clone();

    return new Pointer(typeClone, m_typeName);
}

bool
Pointer::SetProperty(const OString &name, const OString &value)
{
    if (m_type == NULL)
        return false;

    return m_type->SetProperty(name, value);
}

Logging::Node *
Pointer::ToNode(const void *start, bool deep, IPropertyProvider *propProv) const
{
    const void **ptr = (const void **) start;

    Logging::Element *el = new Logging::Element("Value");

    el->AddField("Type", m_typeName);

    OOStringStream ss;
    if (*ptr != NULL)
        ss << hex << "0x" << *ptr;
    else
        ss << "NULL";

    el->AddField("Value", ss.str());

    if (*ptr != NULL && deep)
    {
        Logging::Node *node = m_type->ToNode(*ptr, true, propProv);
        if (node != NULL)
            el->AppendChild(node);
    }

    return el;
}

OString
Pointer::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const void **ptr = (const void **) start;
    if (*ptr != NULL)
    {
        if (deep)
        {
            ss << m_type->ToString(*ptr, true, propProv);
        }
        else
        {
            ss << "0x" << *ptr;
        }
    }
    else
    {
        ss << "NULL";
    }

    return ss.str();
}

bool
Integer::SetProperty(const OString &name, const OString &value)
{
    if (name != "Hex")
        return false;

    if (value == "true")
        m_hex = true;
    else if (value == "false")
        m_hex = false;
    else
        return false;

    return true;
}

OString
UInt16::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const unsigned short *wPtr = reinterpret_cast<const unsigned short *>(start);

    if (m_hex)
        ss << "0x" << hex;
    else
        ss << dec;

    ss << static_cast<unsigned int>(*wPtr);

    return ss.str();
}

OString
UInt16BE::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const unsigned short *wPtr = reinterpret_cast<const unsigned short *>(start);

    if (m_hex)
        ss << "0x" << hex;
    else
        ss << dec;

    unsigned int dw = ((*wPtr >> 8) & 0x00FF) |
                      ((*wPtr << 8) & 0xFF00);
    ss << dw;

    return ss.str();
}

OString
UInt32::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const unsigned int *dwPtr = reinterpret_cast<const unsigned int *>(start);

    if (m_hex)
        ss << "0x" << hex;
    else
        ss << dec;

    ss << *dwPtr;

    return ss.str();
}

bool
UInt32::ToInt(const void *start, int &result) const
{
    const int *dwPtr = reinterpret_cast<const int *>(start);
    
    result = *dwPtr;

    return true;
}

OString
UInt32BE::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const unsigned int *dwPtr = reinterpret_cast<const unsigned int *>(start);

    if (m_hex)
        ss << "0x" << hex;
    else
        ss << dec;

    unsigned int dw = (*dwPtr >> 24) & 0x000000FF |
                      (*dwPtr >>  8) & 0x0000FF00 |
                      (*dwPtr <<  8) & 0x00FF0000 |
                      (*dwPtr << 24) & 0xFF000000;
    ss << dw;

    return ss.str();
}

ByteArray::ByteArray(int size)
    : BaseMarshaller("ByteArray"), m_size(size)
{
}

ByteArray::ByteArray(const OString &sizePropertyBinding)
    : BaseMarshaller("ByteArray"), m_size(0)
{
    SetPropertyBinding("Size", sizePropertyBinding);
}

bool
ByteArray::SetProperty(const OString &name, const OString &value)
{
    if (name != "Size")
        return false;

    char *endPtr = NULL;
    int iVal = strtol(value.c_str(), &endPtr, 0);
    if (endPtr == value.c_str())
    {
        SetPropertyBinding("Size", value);
    }
    else
    {
        m_size = iVal;
    }

    return true;
}

Logging::Node *
ByteArray::ToNode(const void *start, bool deep, IPropertyProvider *propProv) const
{
    int size = m_size;

    if (size <= 0)
    {
        if (HasPropertyBinding("Size"))
        {
            propProv->QueryForProperty(GetPropertyBinding("Size"), size);
        }
    }

    Logging::DataNode *node = NULL;
    
    if (size > 0)
    {
        node = new Logging::DataNode("Value");
        node->AddField("Type", "ByteArray");

        OOStringStream ss;
        ss << size;
        node->AddField("Size", ss.str());

        node->SetData(start, size);
    }

    return node;
}

OString
ByteArray::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    return "TODO";
}

OString
AsciiString::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const char *strPtr = static_cast<const char *>(start);

    return strPtr;
}

OString
UnicodeString::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const WCHAR *strPtr = static_cast<const WCHAR *>(start);
    unsigned int len = static_cast<unsigned int>(wcslen(strPtr));

    OString result;
    result.resize(len);
    WideCharToMultiByte(CP_ACP, 0, strPtr, -1, const_cast<char *>(result.data()),
                        static_cast<int>(result.size()), NULL, NULL);

    return result;
}

Enumeration::Enumeration(const char *name, const char *firstName, ...)
    : UInt32(true)
{
    m_typeName = name;

    va_list args;
    va_start(args, firstName);

    const char *key = firstName;
    while (key != NULL)
    {
        DWORD value = va_arg(args, DWORD);

        m_defs[value] = key;

        key = va_arg(args, const char *);
    }

    va_end(args);
}

Logging::Node *
Enumeration::ToNode(const void *start, bool deep, IPropertyProvider *propProv) const
{
    Logging::Element *el = new Logging::Element("Value");

    el->AddField("Type", "Enum");
    el->AddField("SubType", m_typeName);
    el->AddField("Value", ToString(start, false, propProv));

    return el;
}

OString
Enumeration::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    const DWORD *dwPtr = reinterpret_cast<const DWORD *>(start);

    OMap<DWORD, OString>::Type::const_iterator it = m_defs.find(*dwPtr);
    if (it != m_defs.end())
    {
        return it->second;
    }
    else
    {
        return UInt32::ToString(start, deep, propProv);
    }
}

Structure::Structure(const char *name, const char *firstFieldName, ...)
    : BaseMarshaller(name)
{
    va_list args;
    va_start(args, firstFieldName);

    Initialize(firstFieldName, args);

    va_end(args);
}

Structure::Structure(const char *name, const char *firstFieldName, va_list args)
    : BaseMarshaller(name), m_size(0)
{
    Initialize(firstFieldName, args);
}

Structure::~Structure()
{
    for (FieldsVector::iterator iter = m_fields.begin(); iter != m_fields.end(); iter++)
    {
        delete *iter;
    }
}

void
Structure::Initialize(const char *firstFieldName, va_list args)
{
    const char *fieldName = firstFieldName;
    while (fieldName != NULL)
    {
        DWORD offset = va_arg(args, DWORD);
        BaseMarshaller *marshaller = va_arg(args, BaseMarshaller *);

        AddField(new StructureField(fieldName, offset, marshaller));

        fieldName = va_arg(args, const char *);
    }
}

BaseMarshaller *
Structure::Clone() const
{
    Structure *clone = new Structure(m_typeName.c_str(), NULL);

    for (FieldsVector::const_iterator iter = m_fields.begin(); iter != m_fields.end(); iter++)
    {
        clone->AddField(new StructureField(**iter));
    }

    return clone;
}

void
Structure::AddField(StructureField *field)
{
    m_fields.push_back(field);
    m_size += field->GetMarshaller()->GetSize();
}

Logging::Node *
Structure::ToNode(const void *start, bool deep, IPropertyProvider *propProv) const
{
    const void **ptr = (const void **) start;

    Logging::Element *structElement = new Logging::Element("Value");

    structElement->AddField("Type", "Struct");
    structElement->AddField("SubType", m_typeName);

    for (unsigned int i = 0; i < m_fields.size(); i++)
    {
        const StructureField *field = m_fields[i];

        const void *fieldPtr = reinterpret_cast<const char *>(start) + field->GetOffset();

        Logging::Element *fieldElement = new Logging::Element("Field");
        structElement->AppendChild(fieldElement);

        fieldElement->AddField("Name", field->GetName());
        Logging::Node *valueNode = field->GetMarshaller()->ToNode(fieldPtr, true, propProv);
        if (valueNode != NULL)
            fieldElement->AppendChild(valueNode);
    }

    return structElement;
}

OString
Structure::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    ss << "[ ";

    for (unsigned int i = 0; i < m_fields.size(); i++)
    {
        const StructureField *field = m_fields[i];

        const void *fieldPtr = reinterpret_cast<const char *>(start) + field->GetOffset();

        if (i > 0)
            ss << ", ";

        ss << field->GetName() << "=" << field->GetMarshaller()->ToString(fieldPtr, true, propProv);
    }

    ss << " ]";

    return ss.str();
}

StructurePtr::StructurePtr(const char *firstFieldName, ...)
{
    va_list args;
    va_start(args, firstFieldName);

    m_type = new Structure(firstFieldName, args);

    va_end(args);
}

namespace Winsock {

OString
Ipv4InAddr::ToString(const void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const unsigned char *addr = reinterpret_cast<const unsigned char *>(start);

    ss << (DWORD) addr[0] << "."
       << (DWORD) addr[1] << "."
       << (DWORD) addr[2] << "."
       << (DWORD) addr[3];

    return ss.str();
}

} // namespace Winsock

} // namespace Marshaller

} // namespace InterceptPP