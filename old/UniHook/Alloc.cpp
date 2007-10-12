//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

#include "stdafx.h"

void *
ospy_malloc(size_t size)
{
    return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, size);
}

void *
ospy_realloc(void *ptr, size_t new_size)
{
    return HeapReAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, ptr, new_size);
}

void
ospy_free(void *ptr)
{
    HeapFree(GetProcessHeap(), 0, ptr);
}

char *
ospy_strdup(const char *str)
{
    char *s;
    size_t size = strlen(str) + 1;

    s = (char *) ospy_malloc(size);
    memcpy(s, str, size);

    return s;
}
