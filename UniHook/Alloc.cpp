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
