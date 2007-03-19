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

OString
UInt32::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    const DWORD *dwPtr = reinterpret_cast<const DWORD *>(start);

    if (m_hex)
        ss << "0x" << hex;
    else
        ss << dec;

    ss << *dwPtr;

    return ss.str();
}

OString
UInt32Ptr::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    const DWORD **dwPtr = (const DWORD **) start;
    if (*dwPtr != NULL)
    {
        if (deep)
        {
            if (m_hex)
                ss << "0x" << hex;
            else
                ss << dec;

            ss << **dwPtr;
        }
        else
            ss << "0x" << *dwPtr;
    }
    else
    {
        ss << "NULL";
    }

    return ss.str();
}

OString
AsciiStringPtr::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    const char **strPtr = (const char **) start;
    if (*strPtr != NULL)
    {
        if (deep)
        {
            ss << "\"" << *strPtr << "\"";
        }
        else
        {
            ss << "0x" << strPtr;
        }
    }
    else
    {
        ss << "NULL";
    }

    return ss.str();
}

OString
UnicodeStringPtr::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    // FIXME: use C++ style cast here
    const WCHAR **strPtr = (const WCHAR **) start;
    if (*strPtr != NULL)
    {
        if (deep)
        {
            int bufSize = static_cast<int>(wcslen(*strPtr)) + 1;
            char *buf = new char[bufSize];

            WideCharToMultiByte(CP_ACP, 0, *strPtr, -1, buf, bufSize, NULL, NULL);

            ss << "\"" << buf << "\"";

            delete buf;
        }
        else
        {
            ss << "0x" << strPtr;
        }
    }
    else
    {
        ss << "NULL";
    }

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

StructurePtr::StructurePtr(const char *firstFieldName, ...)
{
    va_list args;
    va_start(args, firstFieldName);

    const char *fieldName = firstFieldName;
    while (fieldName != NULL)
    {
        DWORD offset = va_arg(args, DWORD);
        BaseMarshaller *marshaller = va_arg(args, BaseMarshaller *);

        m_fields.push_back(StructureField(fieldName, offset, marshaller));

        fieldName = va_arg(args, const char *);
    }

    va_end(args);
}

OString
StructurePtr::ToString(const void *start, bool deep) const
{
    OOStringStream ss;

    const void **structPtr = (const void **) start;
    if (*structPtr != NULL)
    {
        if (deep)
        {
            ss << "[ ";

            for (unsigned int i = 0; i < m_fields.size(); i++)
            {
                const StructureField &field = m_fields[i];

                const void *fieldPtr = reinterpret_cast<const char *>(*structPtr) + field.GetOffset();

                if (i > 0)
                    ss << ", ";

                ss << field.GetName() << "=" << field.GetMarshaller()->ToString(fieldPtr, true);
            }

            ss << " ]";
        }
        else
        {
            ss << "0x" << *structPtr;
        }
    }
    else
    {
        ss << "NULL";
    }

    return ss.str();
}

} // namespace Marshaller

} // namespace TrampoLib