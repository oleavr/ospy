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

typedef struct {
    char *moduleName;
    int startOffset;
    char *signature;
} SignatureSpec;

typedef enum {
    TOKEN_TYPE_UNKNOWN = 0,
    TOKEN_TYPE_LITERAL = 1,
    TOKEN_TYPE_IGNORE = 2,
} SignatureTokenType;

class SignatureToken : public BaseObject
{
public:
    SignatureToken(SignatureTokenType type=TOKEN_TYPE_UNKNOWN, int length=0)
        : m_type(type), m_length(length)
    {}

    SignatureTokenType GetType() const { return m_type; }
    void SetType(SignatureTokenType type) { m_type = type; }

    unsigned int GetLength() const
    {
        if (m_type == TOKEN_TYPE_LITERAL)
            return static_cast<unsigned int>(m_data.size());
        else
            return m_length;
    }
    void SetLength(int length) { m_length = length; }

    const char *GetData() const { return m_data.data(); }

    SignatureToken &operator+=(char b) { m_data += b; return *this; }

protected:
    SignatureTokenType m_type;
    unsigned int m_length;
    OString m_data;
};

class Signature : public BaseObject
{
public:
    Signature(const OString &spec);

    void Initialize(const OString &spec);

    unsigned int GetLength() const { return m_length; }

    unsigned int GetTokenCount() const { return static_cast<unsigned int>(m_tokens.size()); }
    const SignatureToken &GetTokenByIndex(int index) const { return m_tokens[index]; }

    int GetLongestTokenIndex() const { return m_longestIndex; }
    const SignatureToken &GetLongestToken() const { return m_tokens[m_longestIndex]; }
    int GetLongestTokenOffset() const { return m_longestOffset; }

    const SignatureToken &operator[](int index) const { return m_tokens[index]; }

protected:
    OVector<SignatureToken>::Type m_tokens;
    unsigned int m_length;
    int m_longestIndex;
    int m_longestOffset;

    void ParseSpec(const OString &spec);
};

class SignatureMatcher : public BaseObject
{
public:
    static SignatureMatcher *Instance()
    {
        static SignatureMatcher *matcher = new SignatureMatcher();
        return matcher;
    }

    OVector<void *>::Type FindInRange(const Signature &sig, void *base, unsigned int size);
    void *FindUniqueInRange(const Signature &sig, void *base, unsigned int size);
    void *FindUniqueInModule(const Signature &sig, OICString moduleName);

protected:
    bool MatchesSignature(const Signature &sig, void *base);
};

} // namespace InterceptPP
