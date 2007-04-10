#pragma once

#include <map>
#include <string>

void get_module_name_for_address(LPVOID address, char *buf, int buf_size);
BOOL get_module_base_and_size(const char *module_name, LPVOID *base, DWORD *size, char **error);

std::string hexdump(void *x, unsigned long len, unsigned int w);

template <class kT, class vT>
struct HashTable
{
	typedef std::map<kT, vT, std::less<kT>, MyAlloc<std::pair<kT, vT>>> Type;
};