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

#include "stdafx.h"
#include <cctype>
#include "Core.h"
#include "Signature.h"
#include "Util.h"

#pragma warning( disable : 4312 )

namespace InterceptPP {

Signature::Signature(const OString &spec)
{
    Initialize(spec);
}

void
Signature::Initialize(const OString &spec)
{
    m_tokens.clear();
    m_length = 0;
    m_longestIndex = -1;
    m_longestOffset = -1;

    ParseSpec(spec);

    int longest = -1;
    int offset = 0;
    for (unsigned int i = 0; i < m_tokens.size(); i++)
    {
        SignatureToken &t = m_tokens[i];
        if (t.GetType() == TOKEN_TYPE_LITERAL)
        {
            int len = t.GetLength();
            if (len > longest)
            {
                longest = len;
                m_longestIndex = i;
                m_longestOffset = offset;
            }
        }

        offset += t.GetLength();
    }

    if (m_longestIndex < 0)
        throw Error("no tokens found");
}

void
Signature::ParseSpec(const OString &spec)
{
    OIStringStream iss(spec, OIStringStream::in);

    while (iss.good())
    {
        if (isxdigit(iss.peek()))
        {
            SignatureToken token(TOKEN_TYPE_LITERAL);

            do
            {
                char buf[3];

                if (iss.get(buf, sizeof(buf)).good())
                {
                    OIStringStream bufIss(buf, OIStringStream::in);

                    unsigned int v;
                    bufIss >> hex >> v;

                    token += static_cast<char>(v);
                }

                while (isspace(iss.peek()) && iss.good())
                    iss.ignore(1);
            }
            while (isxdigit(iss.peek()) && iss.good());

            int length = token.GetLength();
            if (length == 0)
                throw Error("invalid signature spec");

            m_length += token.GetLength();
            m_tokens.push_back(token);
        }
        else
        {
            int ignores = 0;

            do
            {
                char c = iss.peek();

                if (isspace(c) || c == 'x' || c == 'X')
                {
                    iss.ignore(1);
                    if (c == 'x' || c == 'X')  ignores++;
                }
                else
                {
                    break;
                }
            }
            while (iss.good());

            if (ignores > 0)
            {
                if (ignores % 2 != 0)
                    throw Error("unbalanced questionmarks");

                SignatureToken token(TOKEN_TYPE_IGNORE, ignores / 2);

                m_length += token.GetLength();
                m_tokens.push_back(token);
            }
        }
    }
}

OVector<void *>::Type
SignatureMatcher::FindInRange(const Signature &sig, void *base, unsigned int size)
{
    OVector<void *>::Type matches;
    unsigned char *p = static_cast<unsigned char *>(base);
    unsigned char *maxP = p + size - sig.GetLength();

    const SignatureToken &t = sig.GetLongestToken();
    const char *longestTokenData = t.GetData();
    unsigned int longestTokenLen = t.GetLength();
    int longestTokenOffset = sig.GetLongestTokenOffset();

    for (; p <= maxP; p++)
    {
        if (memcmp(p, longestTokenData, longestTokenLen) == 0)
        {
            void *candidateBase = p - longestTokenOffset;

            if (MatchesSignature(sig, candidateBase))
            {
                matches.push_back(candidateBase);

                // Skip ahead
                p = static_cast<unsigned char *>(candidateBase) + sig.GetLength();
            }
        }
    }

    return matches;
}

void *
SignatureMatcher::FindUniqueInRange(const Signature &sig, void *base, unsigned int size)
{
    OVector<void *>::Type matches = FindInRange(sig, base, size);

    if (matches.size() == 0)
        throw Error("No matches found");
    else if (matches.size() > 1)
        throw Error("More than one match found");

    return matches[0];
}

void *
SignatureMatcher::FindUniqueInModule(const Signature &sig, OICString moduleName)
{
    OModuleInfo mi = Util::Instance()->GetModuleInfo(moduleName);

    return FindUniqueInRange(sig, reinterpret_cast<void *>(mi.startAddress), mi.endAddress - mi.startAddress);
}

bool
SignatureMatcher::MatchesSignature(const Signature &sig, void *base)
{
    unsigned char *p = static_cast<unsigned char *>(base);

    for (unsigned int i = 0; i < sig.GetTokenCount(); i++)
    {
        const SignatureToken &t = sig[i];

        if (t.GetType() == TOKEN_TYPE_LITERAL)
        {
            if (memcmp(p, t.GetData(), t.GetLength()) != 0)
            {
                return false;
            }
        }

        p += t.GetLength();
    }

    return true;
}

} // namespace InterceptPP
