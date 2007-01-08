#pragma once

typedef enum {
    TOKEN_TYPE_LITERAL = 0,
    TOKEN_TYPE_IGNORE = 1,
} SignatureTokenType;

class CSignatureToken
{
public:
    SignatureTokenType type;
    int length;
    unsigned char *data;
} ;

typedef struct {
    char *moduleName;
    char *signature;
} FunctionSignature;

class CSigMatcher
{
	static void FindSignatureInRange(const FunctionSignature *sig, LPVOID base, DWORD size, LPVOID *firstMatch, DWORD *numMatches);
	static void FindUniqueSignatureInModule(const FunctionSignature *sig, const char *moduleName, LPVOID *address);
	static void FindUniqueSignature(const FunctionSignature *sig, LPVOID *address);
};
