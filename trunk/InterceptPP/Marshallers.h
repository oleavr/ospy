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

#include "Logging.h"

namespace InterceptPP {

class BaseMarshaller : public BaseObject
{
public:
    BaseMarshaller(const OString &typeName)
        : m_typeName(typeName)
    {}
    virtual ~BaseMarshaller() {};

    virtual unsigned int GetSize() const = 0;
    virtual void AppendToElement(Logging::Element *parentElement, const void *start, bool deep) const;
    virtual OString ToString(const void *start, bool deep) const = 0;

protected:
    OString m_typeName;
};

namespace Marshaller {

class Pointer : public BaseMarshaller
{
public:
    Pointer(BaseMarshaller *type=NULL);
    virtual ~Pointer();

    virtual unsigned int GetSize() const { return sizeof(void *); }
    virtual void AppendToElement(Logging::Element *parentElement, const void *start, bool deep) const;
	virtual OString ToString(const void *start, bool deep) const;

protected:
    BaseMarshaller *m_type;
};

class Integer : public BaseMarshaller
{
public:
    Integer(const OString &typeName, bool hex=false)
        : BaseMarshaller(typeName), m_hex(hex)
    {}

	bool GetFormatHex() const { return m_hex; }
	void SetFormatHex(bool hex) { m_hex = hex; }

protected:
    bool m_hex;
};

class UInt16 : public Integer
{
public:
    UInt16(bool hex=false)
        : Integer("uint16", hex)
    {}

	virtual unsigned int GetSize() const { return sizeof(WORD); }
	virtual OString ToString(const void *start, bool deep) const;
};

class UInt16BE : public Integer
{
public:
    UInt16BE(bool hex=false)
        : Integer("uint16be", hex)
    {}

	virtual unsigned int GetSize() const { return sizeof(WORD); }
	virtual OString ToString(const void *start, bool deep) const;
};

class UInt32 : public Integer
{
public:
    UInt32(bool hex=false)
        : Integer("uint32", hex)
    {}

	virtual unsigned int GetSize() const { return sizeof(DWORD); }
	virtual OString ToString(const void *start, bool deep) const;
};

class UInt32BE : public Integer
{
public:
    UInt32BE(bool hex=false)
        : Integer("uint32be", hex)
    {}

	virtual unsigned int GetSize() const { return sizeof(DWORD); }
	virtual OString ToString(const void *start, bool deep) const;
};

class UInt32Ptr : public Pointer
{
public:
    UInt32Ptr(bool hex=false)
        : Pointer(new UInt32(hex))
    {}
};

class CString : public BaseMarshaller
{
public:
    CString(const OString &typeName, unsigned int elementSize, int length)
        : BaseMarshaller(typeName),
          m_elementSize(elementSize),
          m_length(length)
    {}

    virtual unsigned int GetSize() const { return m_elementSize * (m_length + 1); }

protected:
    unsigned int m_elementSize;
    int m_length;
};

class AsciiString : public CString
{
public:
    AsciiString(int length=-1)
        : CString("asciistring", sizeof(CHAR), length)
    {}

    virtual OString ToString(const void *start, bool deep) const;
};

class AsciiStringPtr : public Pointer
{
public:
    AsciiStringPtr()
        : Pointer(new AsciiString())
    {}
};

class UnicodeString : public CString
{
public:
    UnicodeString(int length=-1)
        : CString("unicodestring", sizeof(WCHAR), length)
    {}

    virtual OString ToString(const void *start, bool deep) const;
};

class UnicodeStringPtr : public Pointer
{
public:
    UnicodeStringPtr()
        : Pointer(new UnicodeString())
    {}
};

class Enumeration : public UInt32
{
public:
    Enumeration(const char *name, const char *firstName, ...);

    void AppendToElement(Logging::Element *parentElement, const void *start, bool deep) const;
    virtual OString ToString(const void *start, bool deep) const;

protected:
    OMap<DWORD, OString>::Type m_defs;
};

class StructureField : public BaseObject
{
public:
    StructureField(const OString &name, DWORD offset, BaseMarshaller *marshaller)
        : m_name(name), m_offset(offset), m_marshaller(marshaller)
    {}

    const OString &GetName() const { return m_name; }
    DWORD GetOffset() const { return m_offset; }
    const BaseMarshaller *GetMarshaller() const { return m_marshaller; }

protected:
    OString m_name;
    DWORD m_offset;
    BaseMarshaller *m_marshaller;
};

class Structure : public BaseMarshaller
{
public:
    Structure(const char *name, const char *firstFieldName, ...);
    Structure(const char *name, const char *firstFieldName, va_list args);

    virtual unsigned int GetSize() const { return sizeof(void *); }
    virtual void AppendToElement(Logging::Element *parentElement, const void *start, bool deep) const;
    virtual OString ToString(const void *start, bool deep) const;

protected:
    OVector<StructureField>::Type m_fields;

    void Initialize(const char *firstFieldName, va_list args);
};

class StructurePtr : public Pointer
{
public:
    StructurePtr(const char *firstFieldName, ...);
};

namespace Registry {

class KeyHandle : public Enumeration
{
public:
    KeyHandle()
        : Enumeration("keyhandle",
                      "HKEY_CLASSES_ROOT", 0x80000000,
                      "HKEY_CURRENT_USER", 0x80000001,
                      "HKEY_LOCAL_MACHINE", 0x80000002,
                      "HKEY_USERS", 0x80000003,
                      "HKEY_DYN_DATA", 0x80000006,
                      NULL)
    {}
};

} // namespace Registry

namespace Winsock {

class Ipv4InAddr : public BaseMarshaller
{
public:
    Ipv4InAddr()
        : BaseMarshaller("ipv4inaddr")
    {}

	virtual unsigned int GetSize() const { return sizeof(DWORD); }
	virtual OString ToString(const void *start, bool deep) const;
};

class Ipv4Sockaddr : public Structure
{
public:
    Ipv4Sockaddr()
        : Structure("ipv4sockaddr",
                    "sin_family", 0, new UInt16(),
                    "sin_port", 2, new UInt16BE(),
                    "sin_addr", 4, new Ipv4InAddr(),
                    NULL)
    {}
};

class Ipv4SockaddrPtr : public Pointer
{
public:
    Ipv4SockaddrPtr()
        : Pointer(new Ipv4Sockaddr())
    {}
};

} // namespace Winsock

} // namespace Marshaller

} // namespace InterceptPP