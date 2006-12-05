#pragma once

typedef struct {
    char *module_name;
    char *signature;
} FunctionSignature;

BOOL find_signature_in_range(const FunctionSignature *sig, LPVOID base, DWORD size, LPVOID *first_match, DWORD *num_matches, char **error);
BOOL find_unique_signature_in_module(const FunctionSignature *sig, const char *module_name, LPVOID *address, char **error);
BOOL find_unique_signature(const FunctionSignature *sig, LPVOID *address, char **error);
