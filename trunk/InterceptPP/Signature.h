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