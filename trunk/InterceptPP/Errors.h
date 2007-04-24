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

//
// We can't use std::runtime_error because it uses std::string with the default allocator...
//
class Error : public exception
{
public:
    Error(const OString &message)
    {
        m_what = message;
    }

    Error(const WCHAR *message)
    {
        int size = WideCharToMultiByte(CP_UTF8, 0, message, -1, NULL, 0, NULL, NULL);
        m_what.resize(size);

        WideCharToMultiByte(CP_UTF8, 0, message, -1, const_cast<char *>(m_what.data()),
                            static_cast<int>(m_what.size()), NULL, NULL);

        // Discard the NUL byte
        m_what.resize(size - 1);
    }

    virtual const char* what() const throw()
    {
        return m_what.c_str();
    }

protected:
    OString m_what;
};

class ParserError : public Error
{
public:
    ParserError(const OString &message)
        : Error(message)
    {}

    ParserError(const WCHAR *message)
        : Error(message)
    {}
};

};