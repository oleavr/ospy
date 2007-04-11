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
BaseMarshaller::ToNode(void *start, bool deep, IPropertyProvider *propProv) const
{
    Logging::Element *el = new Logging::Element("value");

    el->AddField("type", m_typeName);
    el->AddField("value", ToString(start, false, propProv));

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
    REGISTER_MARSHALLER(VaList);
    REGISTER_MARSHALLER(UInt8);
    REGISTER_MARSHALLER(UInt8Ptr);
    REGISTER_MARSHALLER(Int8);
    REGISTER_MARSHALLER(Int8Ptr);
    REGISTER_MARSHALLER(UInt16);
    REGISTER_MARSHALLER(UInt16Ptr);
    REGISTER_MARSHALLER(Int16);
    REGISTER_MARSHALLER(Int16Ptr);
    REGISTER_MARSHALLER(UInt32);
    REGISTER_MARSHALLER(UInt32Ptr);
    REGISTER_MARSHALLER(Int32);
    REGISTER_MARSHALLER(Int32Ptr);
    REGISTER_MARSHALLER(ByteArray);
    REGISTER_MARSHALLER(ByteArrayPtr);
    REGISTER_MARSHALLER(AsciiString);
    REGISTER_MARSHALLER(AsciiStringPtr);
    REGISTER_MARSHALLER(UnicodeString);
    REGISTER_MARSHALLER(UnicodeStringPtr);
    REGISTER_MARSHALLER(UnicodeFormatString);
    REGISTER_MARSHALLER(UnicodeFormatStringPtr);
    REGISTER_MARSHALLER(Ipv4InAddr);
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
Pointer::ToNode(void *start, bool deep, IPropertyProvider *propProv) const
{
    void **ptr = static_cast<void **>(start);

    Logging::Element *el = new Logging::Element("value");

    el->AddField("type", m_typeName);

    OOStringStream ss;
    if (*ptr != NULL)
        ss << hex << "0x" << *ptr;
    else
        ss << "NULL";

    el->AddField("value", ss.str());

    if (*ptr != NULL && deep)
    {
        Logging::Node *node = m_type->ToNode(*ptr, true, propProv);
        if (node != NULL)
            el->AppendChild(node);
    }

    return el;
}

OString
Pointer::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    void **ptr = static_cast<void **>(start);
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
Pointer::ToPointer(void *start, void *&result) const
{
    void **ptr = static_cast<void **>(start);
    result = *ptr;
    return true;
}

Logging::Node *
VaList::ToNode(void *start, bool deep, IPropertyProvider *propProv) const
{
    Logging::Element *el = new Logging::Element("value");
    el->AddField("type", m_typeName);
    el->AddField("value", ToString(start, deep, propProv));

    return el;
}

OString
VaList::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    void **ptr = static_cast<void **>(start);

    OOStringStream ss;
    if (*ptr != NULL)
        ss << hex << "0x" << *ptr;
    else
        ss << "NULL";

    return ss.str();
}

bool
VaList::ToVaList(void *start, va_list &result) const
{
    va_list *ptr = static_cast<va_list *>(start);
    result = *ptr;
    return true;
}

template <class T> bool
Integer<T>::SetProperty(const OString &name, const OString &value)
{
    if (name == "hex")
    {
        if (value == "true")
            m_hex = true;
        else if (value == "false")
            m_hex = false;
        else
            return false;
    }
    else if (name == "endian")
    {
        if (value == "big")
            m_bigEndian = true;
        else if (value == "little")
            m_bigEndian = false;
        else
            return false;
    }
    else
    {
        return false;
    }

    return true;
}

template <class T> OString
Integer<T>::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    int v;
    ToInt(start, v);

    if (m_hex && v != 0)
        ss << "0x" << hex;
    else
        ss << dec;

    T i = v;

    if (m_sign)
        ss << static_cast<int>(i);
    else
        ss << static_cast<unsigned int>(i);

    return ss.str();
}

template <class T> bool
Integer<T>::ToInt(void *start, int &result) const
{
    T i = *(reinterpret_cast<T *>(start));

    if (m_bigEndian)
    {
        i = ToLittleEndian(i);
    }

    result = i;
    return true;
}

ByteArray::ByteArray(int size)
    : BaseMarshaller("ByteArray"), m_size(size)
{
}

ByteArray::ByteArray(const OString &sizePropertyBinding)
    : BaseMarshaller("ByteArray"), m_size(0)
{
    SetPropertyBinding("size", sizePropertyBinding);
}

bool
ByteArray::SetProperty(const OString &name, const OString &value)
{
    if (name != "size")
        return false;

    char *endPtr = NULL;
    int iVal = strtol(value.c_str(), &endPtr, 0);
    if (endPtr == value.c_str())
    {
        SetPropertyBinding("size", value);
    }
    else
    {
        m_size = iVal;
    }

    return true;
}

Logging::Node *
ByteArray::ToNode(void *start, bool deep, IPropertyProvider *propProv) const
{
    int size = m_size;

    if (size <= 0)
    {
        if (HasPropertyBinding("size"))
        {
            propProv->QueryForProperty(GetPropertyBinding("size"), size);
        }
    }

    Logging::DataNode *node = NULL;
    
    if (size > 0)
    {
        node = new Logging::DataNode("value");
        node->AddField("type", "ByteArray");

        OOStringStream ss;
        ss << size;
        node->AddField("size", ss.str());

        node->SetData(start, size);
    }

    return node;
}

OString
ByteArray::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    return "TODO";
}

OString
AsciiString::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const char *strPtr = static_cast<const char *>(start);

    return strPtr;
}

OString
UnicodeString::ToString(void *start, bool deep, IPropertyProvider *propProv) const
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

bool
UnicodeFormatString::SetProperty(const OString &name, const OString &value)
{
    if (name == "vaList" || name == "vaStart")
    {
        SetPropertyBinding(name, value);
        return true;
    }

    return false;
}

OString
UnicodeFormatString::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    WCHAR *fmtPtr = static_cast<WCHAR *>(start);

    bool success = false;
    va_list args;

    if (HasPropertyBinding("vaList"))
    {
        success = propProv->QueryForProperty(GetPropertyBinding("vaList"), args);
    }
    else if (HasPropertyBinding("vaStart"))
    {
        WCHAR **start;

        if (propProv->QueryForProperty(GetPropertyBinding("vaStart"), reinterpret_cast<void *&>(start)))
        {
            va_start(args, *start);
            success = true;
        }
    }

    WCHAR buf[2048], *p;

    if (success)
    {
        vswprintf(buf, sizeof(buf), fmtPtr, args);
        buf[2047] = '\0';
        p = buf;
    }
    else
    {
        p = fmtPtr;
    }

    unsigned int len = static_cast<unsigned int>(wcslen(p));

    OString result;
    result.resize(len);
    WideCharToMultiByte(CP_ACP, 0, p, -1, const_cast<char *>(result.data()),
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

bool
Enumeration::AddMember(const OString &name, DWORD value)
{
    bool result = true;

    if (m_defs.find(value) == m_defs.end())
        m_defs[value] = name;
    else
        result = false;

    return result;
}

Logging::Node *
Enumeration::ToNode(void *start, bool deep, IPropertyProvider *propProv) const
{
    Logging::Element *el = new Logging::Element("value");

    el->AddField("type", "Enum");
    el->AddField("subType", m_typeName);
    el->AddField("value", ToString(start, false, propProv));

    return el;
}

OString
Enumeration::ToString(void *start, bool deep, IPropertyProvider *propProv) const
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
Structure::ToNode(void *start, bool deep, IPropertyProvider *propProv) const
{
    void **ptr = static_cast<void **>(start);

    Logging::Element *structElement = new Logging::Element("value");

    structElement->AddField("type", "Struct");
    structElement->AddField("subType", m_typeName);

    for (unsigned int i = 0; i < m_fields.size(); i++)
    {
        const StructureField *field = m_fields[i];

        void *fieldPtr = reinterpret_cast<char *>(start) + field->GetOffset();

        Logging::Element *fieldElement = new Logging::Element("field");
        structElement->AppendChild(fieldElement);

        fieldElement->AddField("name", field->GetName());
        Logging::Node *valueNode = field->GetMarshaller()->ToNode(fieldPtr, true, propProv);
        if (valueNode != NULL)
            fieldElement->AppendChild(valueNode);
    }

    return structElement;
}

OString
Structure::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    ss << "[ ";

    for (unsigned int i = 0; i < m_fields.size(); i++)
    {
        const StructureField *field = m_fields[i];

        void *fieldPtr = reinterpret_cast<char *>(start) + field->GetOffset();

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

OString
Ipv4InAddr::ToString(void *start, bool deep, IPropertyProvider *propProv) const
{
    OOStringStream ss;

    const unsigned char *addr = reinterpret_cast<const unsigned char *>(start);

    ss << (DWORD) addr[0] << "."
       << (DWORD) addr[1] << "."
       << (DWORD) addr[2] << "."
       << (DWORD) addr[3];

    return ss.str();
}

} // namespace Marshaller

} // namespace InterceptPP