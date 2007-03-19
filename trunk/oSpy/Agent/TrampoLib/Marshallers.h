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

namespace TrampoLib {

class BaseMarshaller : public BaseObject
{
public:
    virtual ~BaseMarshaller() {};

    virtual unsigned int GetSize() const = 0;
    virtual OString ToString(const void *start, bool deep) const = 0;
};

namespace Marshaller {

class Integer : public BaseMarshaller
{
public:
    Integer(bool hex=false)
        : m_hex(hex)
    {}

protected:
    bool m_hex;
};

class UInt32 : public Integer
{
public:
    UInt32(bool hex=false)
        : Integer(hex)
    {}

	virtual unsigned int GetSize() const { return sizeof(DWORD); }
	virtual OString ToString(const void *start, bool deep) const;
};

class UInt32Ptr : public Integer
{
public:
    UInt32Ptr(bool hex=false)
        : Integer(hex)
    {}

    virtual unsigned int GetSize() const { return sizeof(DWORD *); }
	virtual OString ToString(const void *start, bool deep) const;
};

class AsciiStringPtr : public BaseMarshaller
{
public:
    virtual unsigned int GetSize() const { return sizeof(char *); }
    virtual OString ToString(const void *start, bool deep) const;
};

class UnicodeStringPtr : public BaseMarshaller
{
public:
    virtual unsigned int GetSize() const { return sizeof(char *); }
    virtual OString ToString(const void *start, bool deep) const;
};

class Enumeration : public UInt32
{
public:
    Enumeration(const char *firstName, ...);

    virtual OString ToString(const void *start, bool deep) const;

protected:
    OMap<DWORD, OString>::Type m_defs;
};

namespace Registry {

class KeyHandle : public Enumeration
{
public:
    KeyHandle()
        : Enumeration("HKEY_CLASSES_ROOT", 0x80000000,
                      "HKEY_CURRENT_USER", 0x80000001,
                      "HKEY_LOCAL_MACHINE", 0x80000002,
                      "HKEY_USERS", 0x80000003,
                      "HKEY_DYN_DATA", 0x80000006,
                      NULL)
    {}
};

} // namespace Registry

} // namespace Marshaller

} // namespace TrampoLib