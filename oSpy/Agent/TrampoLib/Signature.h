//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WI
//

#pragma once

namespace TrampoLib {

typedef struct {
    OString moduleName;
    OString signature;
} SignatureSpec;

typedef enum {
    TOKEN_TYPE_LITERAL = 0,
    TOKEN_TYPE_IGNORE = 1,
} SignatureTokenType;

typedef struct {
    SignatureTokenType type;
    int length;
    unsigned char *data;
} SignatureToken;

class Signature : BaseObject
{
public:
    Signature(const SignatureSpec *spec);

    void Initialize(const SignatureSpec *spec);

protected:
    OVector<SignatureToken>::Type m_tokens;
};

class SignatureMatcher : BaseObject
{
public:
    static SignatureMatcher *Instance()
    {
        static SignatureMatcher *matcher = new SignatureMatcher();
        return matcher;
    }

    unsigned int FindInRange(const SignatureSpec *spec, void *base, unsigned int size, void **firstMatch);
    /*
    BOOL find_signature_in_range(const FunctionSignature *sig, LPVOID base, DWORD size, LPVOID *first_match, DWORD *num_matches, char **error);
BOOL find_unique_signature_in_module(const FunctionSignature *sig, const char *module_name, LPVOID *address, char **error);
BOOL find_unique_signature(const FunctionSignature *sig, LPVOID *address, char **error);*/
};

} // namespace TrampoLib