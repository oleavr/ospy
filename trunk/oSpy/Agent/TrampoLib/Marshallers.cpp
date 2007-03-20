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

namespace TrampoLib {

namespace Marshaller {

Pointer::Pointer(BaseMarshaller *type)
    : m_type(type)
{}

Pointer::~Pointer()
{
    if (m_type != NULL)
        delete m_type;
}

OString
Pointer::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    const void **ptr = (const void **) start;
    if (*ptr != NULL)
    {
        if (deep)
        {
            ss << m_type->ToString(*ptr, true);
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

OString
UInt16::ToString(const void *start, bool deep) const
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
UInt16BE::ToString(const void *start, bool deep) const
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
UInt32::ToString(const void *start, bool deep) const
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

OString
UInt32BE::ToString(const void *start, bool deep) const
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

OString
AsciiString::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    const char *strPtr = static_cast<const char *>(start);
    ss << "\"" << *strPtr << "\"";

    return ss.str();
}

OString
UnicodeString::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    const WCHAR *strPtr = static_cast<const WCHAR *>(start);
    unsigned int bufSize = static_cast<unsigned int>(wcslen(strPtr)) + 1;
    char *buf = new char[bufSize];

    WideCharToMultiByte(CP_ACP, 0, strPtr, -1, buf, bufSize, NULL, NULL);

    ss << "\"" << buf << "\"";

    delete buf;

    return ss.str();
}

Enumeration::Enumeration(const char *firstName, ...)
    : UInt32(true)
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

OString
Enumeration::ToString(const void *start, bool deep) const
{
    const DWORD *dwPtr = reinterpret_cast<const DWORD *>(start);

    OMap<DWORD, OString>::Type::const_iterator it = m_defs.find(*dwPtr);
    if (it != m_defs.end())
    {
        return it->second;
    }
    else
    {
        return UInt32::ToString(start, deep);
    }
}

Structure::Structure(const char *firstFieldName, ...)
{
    va_list args;
    va_start(args, firstFieldName);

    Initialize(firstFieldName, args);

    va_end(args);
}

Structure::Structure(const char *firstFieldName, va_list args)
{
    Initialize(firstFieldName, args);
}

void
Structure::Initialize(const char *firstFieldName, va_list args)
{
    const char *fieldName = firstFieldName;
    while (fieldName != NULL)
    {
        DWORD offset = va_arg(args, DWORD);
        BaseMarshaller *marshaller = va_arg(args, BaseMarshaller *);

        m_fields.push_back(StructureField(fieldName, offset, marshaller));

        fieldName = va_arg(args, const char *);
    }
}

OString
Structure::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    ss << "[ ";

    for (unsigned int i = 0; i < m_fields.size(); i++)
    {
        const StructureField &field = m_fields[i];

        const void *fieldPtr = reinterpret_cast<const char *>(start) + field.GetOffset();

        if (i > 0)
            ss << ", ";

        ss << field.GetName() << "=" << field.GetMarshaller()->ToString(fieldPtr, true);
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
Ipv4InAddr::ToString(const void *start, bool deep) const
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

} // namespace TrampoLib