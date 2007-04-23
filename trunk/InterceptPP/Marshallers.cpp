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

void
PropertyOverrides::Add(const OString &propName, const OString &value)
{
    m_overrides[propName] = value;
}

bool
PropertyOverrides::Contains(const OString &propName) const
{
    return m_overrides.find(propName) != m_overrides.end();
}

bool
PropertyOverrides::GetValue(const OString &propName, OString &value) const
{
    OverridesMap::const_iterator it = m_overrides.find(propName);
    if (it == m_overrides.end())
        return false;

    value = it->second;
    return true;
}

bool
PropertyOverrides::GetValue(const OString &propName, int &value) const
{
    OverridesMap::const_iterator it = m_overrides.find(propName);
    if (it == m_overrides.end())
        return false;

    const OString &s = it->second;
    char *endPtr = NULL;
    int iVal = strtol(s.c_str(), &endPtr, 0);
    if (endPtr == s.c_str())
        return false;

    value = iVal;
    return true;
}

bool
PropertyOverrides::GetValue(const OString &propName, unsigned int &value) const
{
    OverridesMap::const_iterator it = m_overrides.find(propName);
    if (it == m_overrides.end())
        return false;

    const OString &s = it->second;
    char *endPtr = NULL;
    unsigned int iVal = strtoul(s.c_str(), &endPtr, 0);
    if (endPtr == s.c_str())
        return false;

    value = iVal;
    return true;
}

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
BaseMarshaller::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
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
    REGISTER_MARSHALLER(Dynamic);
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
    REGISTER_MARSHALLER(Array);
    REGISTER_MARSHALLER(ArrayPtr);
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

Dynamic::Dynamic()
    : BaseMarshaller("Dynamic")
{
    m_fallback = new Int32();
}

Dynamic::Dynamic(const Dynamic &d)
    : BaseMarshaller(d)
{
    m_nameBase = d.m_nameBase;
    m_nameSuffix = d.m_nameSuffix;
    m_fallback = d.m_fallback->Clone();
}

Dynamic::~Dynamic()
{
    delete m_fallback;
}

bool
Dynamic::SetProperty(const OString &name, const OString &value)
{
    if (name == "typeNameBase")
    {
        m_nameBase = value;
    }
    else if (name == "typeNameSuffix")
    {
        m_nameSuffix = value;
    }
    else if (name == "typeNameFallback")
    {
        BaseMarshaller *fallback = Factory::Instance()->CreateMarshaller(value);
        if (fallback == NULL)
            return false;

        delete m_fallback;
        m_fallback = fallback;
    }
    else
    {
        return false;
    }

    return true;
}

Logging::Node *
Dynamic::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    Logging::Element *el = new Logging::Element("value");

    OString subTypeName;
    OString value = ToStringInternal(start, false, propProv, overrides, &subTypeName);

    el->AddField("type", m_typeName);
    el->AddField("subType", subTypeName);
    el->AddField("value", value);

    return el;
}

OString
Dynamic::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    return ToStringInternal(start, deep, propProv, overrides, NULL);
}

OString
Dynamic::ToStringInternal(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides, OString *lastSubTypeName) const
{
    BaseMarshaller *marshaller = CreateMarshaller(propProv);
    if (marshaller == NULL)
    {
        if (lastSubTypeName != NULL)
            *lastSubTypeName = m_fallback->GetName();
        return m_fallback->ToString(start, deep, propProv, overrides);
    }
    else
    {
        if (lastSubTypeName != NULL)
            *lastSubTypeName = marshaller->GetName();
    }

    OString result = marshaller->ToString(start, deep, propProv, overrides);
    delete marshaller;

    return result;
}

BaseMarshaller *
Dynamic::CreateMarshaller(IPropertyProvider *propProv) const
{
    if (m_nameBase.size() == 0)
        return NULL;

    OString s;
    if (!propProv->QueryForProperty(m_nameBase, s))
        return NULL;

    s += m_nameSuffix;

    return Factory::Instance()->CreateMarshaller(s);
}

Pointer::Pointer(BaseMarshaller *type, const OString &ptrTypeName)
    : BaseMarshaller(ptrTypeName), m_type(type)
{}

Pointer::Pointer(const Pointer &p)
    : BaseMarshaller(p), m_type(NULL)
{
    if (p.m_type != NULL)
        m_type = p.m_type->Clone();
}

Pointer::~Pointer()
{
    if (m_type != NULL)
        delete m_type;
}

bool
Pointer::SetProperty(const OString &name, const OString &value)
{
    if (m_type == NULL)
        return false;

    return m_type->SetProperty(name, value);
}

Logging::Node *
Pointer::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
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
        Logging::Node *node = m_type->ToNode(*ptr, true, propProv, overrides);
        if (node != NULL)
            el->AppendChild(node);
    }

    return el;
}

OString
Pointer::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    OOStringStream ss;

    void **ptr = static_cast<void **>(start);
    if (*ptr != NULL)
    {
        if (deep)
        {
            ss << m_type->ToString(*ptr, true, propProv, overrides);
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
VaList::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    Logging::Element *el = new Logging::Element("value");
    el->AddField("type", m_typeName);
    el->AddField("value", ToString(start, deep, propProv));

    return el;
}

OString
VaList::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
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
Integer<T>::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
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
        i = ToLittleEndian(i);

    result = i;
    return true;
}

template <class T> bool
Integer<T>::ToUInt(void *start, unsigned int &result) const
{
    T i = *(reinterpret_cast<T *>(start));
    if (m_bigEndian)
        i = ToLittleEndian(i);

    result = i;
    return true;
}

Array::Array()
    : BaseMarshaller("Array"), m_elType(new UInt8()), m_elCount(0)
{
}

Array::Array(BaseMarshaller *elType, unsigned int elCount)
    : BaseMarshaller("Array"), m_elType(elType), m_elCount(elCount)
{
}

Array::Array(BaseMarshaller *elType, const OString &elCountPropertyBinding)
    : BaseMarshaller("Array"), m_elType(elType), m_elCount(0)
{
    SetPropertyBinding("elementCount", elCountPropertyBinding);
}

Array::Array(const Array &a)
    : BaseMarshaller(a)
{
    m_elType = a.m_elType->Clone();
    m_elCount = a.m_elCount;
}

Array::~Array()
{
    delete m_elType;
}

bool
Array::SetProperty(const OString &name, const OString &value)
{
    if (name == "elementType")
    {
        BaseMarshaller *elType = Factory::Instance()->CreateMarshaller(value);
        if (elType == NULL)
            return false;

        delete m_elType;
        m_elType = elType;
    }
    else if (name == "elementCount")
    {
        char *endPtr = NULL;
        unsigned int iVal = strtoul(value.c_str(), &endPtr, 0);
        if (endPtr == value.c_str())
        {
            SetPropertyBinding("elementCount", value);
        }
        else
        {
            m_elCount = iVal;
        }
    }
    else
    {
        return false;
    }

    return true;
}

unsigned int
Array::GetSize() const
{
    return m_elType->GetSize() * m_elCount;
}

Logging::Node *
Array::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    unsigned int elCount = m_elCount;

    if (elCount == 0)
    {
        if (overrides == NULL || !overrides->GetValue("elementCount", elCount))
        {
            if (HasPropertyBinding("elementCount"))
            {
                propProv->QueryForProperty(GetPropertyBinding("elementCount"), elCount);
            }
        }
    }

    Logging::Element *node = NULL;

    if (elCount > 0)
    {
        node = new Logging::Element("value");
        node->AddField("type", "Array");
        node->AddField("elementType", m_elType->GetName());

        OOStringStream ss;
        ss << elCount;
        node->AddField("elementCount", ss.str());

        unsigned char *p = static_cast<unsigned char *>(start);
        unsigned int elSize = m_elType->GetSize();

        for (unsigned int i = 0; i < elCount; i++)
        {
            Logging::Node *child = m_elType->ToNode(p, deep, propProv, overrides);
            node->AppendChild(child);

            p += elSize;
        }
    }

    return node;
}

OString
Array::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    return "[Array]";
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
ByteArray::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    int size = m_size;

    if (size <= 0)
    {
        if (overrides == NULL || !overrides->GetValue("size", size))
        {
            if (HasPropertyBinding("size"))
            {
                propProv->QueryForProperty(GetPropertyBinding("size"), size);
            }
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
ByteArray::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    return "[ByteArray]";
}

OString
AsciiString::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    OOStringStream ss;

    const char *strPtr = static_cast<const char *>(start);

    return strPtr;
}

OString
UnicodeString::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    OOStringStream ss;

    const WCHAR *strPtr = static_cast<const WCHAR *>(start);

    int size = WideCharToMultiByte(CP_UTF8, 0, strPtr, -1, NULL, 0, NULL, NULL);
    OString result;
    result.resize(size);

    WideCharToMultiByte(CP_UTF8, 0, strPtr, -1, const_cast<char *>(result.data()),
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
UnicodeFormatString::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
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

    int size = WideCharToMultiByte(CP_UTF8, 0, p, -1, NULL, 0, NULL, NULL);
    OString result;
    result.resize(size);

    WideCharToMultiByte(CP_UTF8, 0, p, -1, const_cast<char *>(result.data()),
                        static_cast<int>(result.size()), NULL, NULL);

    return result;
}

Enumeration::Enumeration(const char *name, BaseMarshaller *marshaller, const char *firstName, ...)
    : BaseMarshaller(name), m_marshaller(marshaller)
{
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

Enumeration::Enumeration(const Enumeration &e)
    : BaseMarshaller(e)
{
    m_defs = e.m_defs;
    m_marshaller = e.m_marshaller->Clone();
}

bool
Enumeration::SetProperty(const OString &name, const OString &value)
{
    if (name == "marshalAs")
    {
        BaseMarshaller *marshaller = Factory::Instance()->CreateMarshaller(value);
        if (marshaller == NULL)
            return false;

        delete m_marshaller;
        m_marshaller = marshaller;

        return true;
    }

    return m_marshaller->SetProperty(name, value);
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
Enumeration::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    Logging::Element *el = new Logging::Element("value");

    el->AddField("type", "Enum");
    el->AddField("subType", m_typeName);
    el->AddField("value", ToString(start, deep, propProv, overrides));

    return el;
}

OString
Enumeration::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    unsigned int val;
    m_marshaller->ToUInt(start, val);

    OMap<DWORD, OString>::Type::const_iterator it = m_defs.find(val);
    if (it != m_defs.end())
    {
        return it->second;
    }
    else
    {
        return m_marshaller->ToString(start, deep, propProv, overrides);
    }
}

Structure::Structure(const char *name, const char *firstFieldName, ...)
    : BaseMarshaller(name), m_size(0)
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

Structure::Structure(const Structure &s)
    : BaseMarshaller(s), m_size(0)
{
    for (FieldsVector::const_iterator iter = s.m_fields.begin(); iter != s.m_fields.end(); iter++)
    {
        AddField(new StructureField(**iter));
    }

    m_bindings = s.m_bindings;
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

void
Structure::AddField(StructureField *field)
{
    m_fieldIndexes[field->GetName()] = static_cast<unsigned int>(m_fields.size());
    m_fields.push_back(field);
    m_size += field->GetMarshaller()->GetSize();
}

void
Structure::BindFieldTypePropertyToField(const OString &fieldName, const OString &propName, const OString &srcFieldName)
{
    m_bindings.push_back(FieldTypePropertyBinding(fieldName, propName, srcFieldName));
}

PropertyOverrides **
Structure::GetFieldOverrides(void *start, IPropertyProvider *propProv) const
{
    PropertyOverrides **fieldOverrides = NULL;

    if (m_bindings.size() == 0)
        return NULL;

    fieldOverrides = new PropertyOverrides *[m_fields.size()];
    memset(fieldOverrides, 0, sizeof(PropertyOverrides *) * m_fields.size());

    for (FieldBindingsVector::const_iterator it = m_bindings.begin(); it != m_bindings.end(); it++)
    {
        FieldIndexesMap::const_iterator idxIter = m_fieldIndexes.find(it->GetFieldName());
        FieldIndexesMap::const_iterator srcIdxIter = m_fieldIndexes.find(it->GetSourceFieldName());
        if (idxIter == m_fieldIndexes.end() || srcIdxIter == m_fieldIndexes.end())
            continue;

        unsigned int idx = idxIter->second;
        unsigned int srcIdx = srcIdxIter->second;

        PropertyOverrides *po = fieldOverrides[idx];
        if (po == NULL)
        {
            po = new PropertyOverrides();
            fieldOverrides[idx] = po;
        }

        StructureField *srcField = m_fields[srcIdx];

        void *p = reinterpret_cast<char *>(start) + srcField->GetOffset();
        OString val = srcField->GetMarshaller()->ToString(p, true, propProv);
        po->Add(it->GetPropertyName(), val);
    }

    return fieldOverrides;
}

void
Structure::FreeFieldOverrides(PropertyOverrides **fieldOverrides) const
{
    if (fieldOverrides != NULL)
        delete[] fieldOverrides;
}

Logging::Node *
Structure::ToNode(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    PropertyOverrides **fieldOverrides = GetFieldOverrides(start, propProv);

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

        PropertyOverrides *po = NULL;
        if (fieldOverrides != NULL)
        {
            po = fieldOverrides[i];
        }

        Logging::Node *valueNode = field->GetMarshaller()->ToNode(fieldPtr, true, propProv, po);

        if (po != NULL)
            delete po;

        if (valueNode != NULL)
            fieldElement->AppendChild(valueNode);
    }

    FreeFieldOverrides(fieldOverrides);

    return structElement;
}

OString
Structure::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
{
    return "[Structure]";
}

StructurePtr::StructurePtr(const char *firstFieldName, ...)
{
    va_list args;
    va_start(args, firstFieldName);

    m_type = new Structure(firstFieldName, args);

    va_end(args);
}

OString
Ipv4InAddr::ToString(void *start, bool deep, IPropertyProvider *propProv, PropertyOverrides *overrides) const
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