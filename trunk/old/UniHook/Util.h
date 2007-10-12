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
