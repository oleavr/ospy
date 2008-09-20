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

#include <exception>

namespace InterceptPP {

//
// We can't use std::runtime_error because it uses std::string with the default allocator...
//
class Error : public std::exception
{
public:
    Error(const OString &message)
        : m_what(message)
    {
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
    Error()
    {
    }

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
