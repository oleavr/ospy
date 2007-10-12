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

typedef struct {
    char *module_name;
    char *signature;
} FunctionSignature;

BOOL find_signature_in_range(const FunctionSignature *sig, LPVOID base, DWORD size, LPVOID *first_match, DWORD *num_matches, char **error);
BOOL find_unique_signature_in_module(const FunctionSignature *sig, const char *module_name, LPVOID *address, char **error);
BOOL find_unique_signature(const FunctionSignature *sig, LPVOID *address, char **error);
