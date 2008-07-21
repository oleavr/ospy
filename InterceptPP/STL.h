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

#pragma warning( disable : 4291 4996 )

#include <iomanip>
#include <string>
#include <sstream>
#include <vector>
#include <stack>
#include <list>
#include <map>
#include <limits>
#include "Alloc.h"

using namespace std;

namespace InterceptPP {

class BaseObject
{
public:
    void *operator new(size_t, void *an_address)
    {
        return an_address;
    }

    void *operator new(size_t size)
    {
        return AllocUtils::Malloc(size);
    }

    void operator delete(void *an_address)
    {
        if (an_address)
            AllocUtils::Free(an_address);
    }
};

template <class T>
class MyAlloc : BaseObject
{
public:
    // type definitions
    typedef T        value_type;
    typedef T*       pointer;
    typedef const T* const_pointer;
    typedef T&       reference;
    typedef const T& const_reference;
    typedef size_t   size_type;
    typedef std::ptrdiff_t difference_type;

    // rebind allocator to type U
    template <class U>
    struct rebind
    {
        typedef MyAlloc<U> other;
    };

    // return address of values
    pointer address (reference value) const
    {
        return &value;
    }
    const_pointer address (const_reference value) const
    {
        return &value;
    }

    /* constructors and destructor
    * - nothing to do because the allocator has no state
    */
    MyAlloc() throw()
    {
    }

    MyAlloc(const MyAlloc&) throw()
    {
    }

    template <class U>
    MyAlloc (const MyAlloc<U>&) throw()
    {
    }

    ~MyAlloc() throw()
    {
    }

    // return maximum number of elements that can be allocated
    size_type max_size () const throw()
    {
        return numeric_limits<size_t>::max() / sizeof(T);
    }

    // allocate but don't initialize num elements of type T
    pointer allocate (size_type num, const void* = 0)
    {
        pointer ret (NULL);

        try
        {
            ret = static_cast<pointer>(operator new(num * sizeof(T)));
        }
        catch (bad_alloc &)
        {
            exit(1);
        }

        return ret;
    }

    // initialize elements of allocated storage p with value value
    void construct (pointer p, const T &value)
    {
        // initialize memory with placement new
        try
        {
            new(static_cast<void*>(p))T(value);
        }
        catch (bad_alloc &)
        {
            exit(1);
        }
    }

    // destroy elements of initialized storage p
    void destroy (pointer p)
    {
        // destroy objects by calling their destructor
        p->~T();
    }

    // deallocate storage p of deleted elements
    void deallocate (pointer p, size_type num)
    {
        // print message and deallocate memory with delete
        operator delete(static_cast<void*>(p));
    }
};

// return that all specializations of this allocator are interchangeable
template <class T1, class T2>
bool operator== (const MyAlloc<T1>&, const MyAlloc<T2>&) throw()
{
    return true;
}

template <class T1, class T2>
bool operator!= (const MyAlloc<T1>&, const MyAlloc<T2>&) throw()
{
    return false;
}

struct ci_char_traits : public char_traits<char>
            // just inherit all the other functions
            //  that we don't need to override
{
    static bool eq(char c1, char c2)
    {
        return toupper(c1) == toupper(c2);
    }

    static bool ne(char c1, char c2)
    {
        return toupper(c1) != toupper(c2);
    }

    static bool lt(char c1, char c2)
    {
        return toupper(c1) <  toupper(c2);
    }

    static int compare(const char *s1, const char *s2, size_t n)
    {
        return _memicmp(s1, s2, n);
    }

    static const char *find(const char *s, int n, char a)
    {
        while(n-- > 0 && toupper(*s) != toupper(a)) {
            ++s;
        }

        return s;
    }
};

typedef basic_string<char, char_traits<char>, MyAlloc<char>> OString;
typedef basic_string<wchar_t, char_traits<wchar_t>, MyAlloc<wchar_t>> OWString;

typedef basic_string<char, ci_char_traits, MyAlloc<char>> OICString;

typedef basic_ostringstream<char, char_traits<char>, MyAlloc<char>> OOStringStream;
typedef basic_ostringstream<wchar_t, char_traits<wchar_t>, MyAlloc<wchar_t>> OOWStringStream;

typedef basic_istringstream<char, char_traits<char>, MyAlloc<char>> OIStringStream;

template <class eT>
struct OVector
{
    typedef std::vector<eT, MyAlloc<eT>> Type;
};

template <class eT>
struct OStack
{
    typedef std::stack<eT, std::deque<eT, MyAlloc<eT>>> Type;
};

template <class eT>
struct OList
{
    typedef std::list<eT, MyAlloc<eT>> Type;
};

template <class kT, class vT>
struct OMap
{
    typedef std::map<kT, vT, std::less<kT>, MyAlloc<std::pair<kT, vT>>> Type;
};

} // namespace InterceptPP
