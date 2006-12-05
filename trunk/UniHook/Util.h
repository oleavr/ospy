#pragma once

#include <string>

void *ospy_malloc(size_t size);
void *ospy_realloc(void *ptr, size_t new_size);
void ospy_free(void *ptr);
char *ospy_strdup(const char *str);

void get_module_name_for_address(LPVOID address, char *buf, int buf_size);
BOOL get_module_base_and_size(const char *module_name, LPVOID *base, DWORD *size, char **error);

std::string hexdump(void *x, unsigned long len, unsigned int w);