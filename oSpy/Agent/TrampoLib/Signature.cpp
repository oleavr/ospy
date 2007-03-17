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

#include "stdafx.h"
#include "Core.h"

namespace TrampoLib {

#if 0
static BOOL
parse_signature(const FunctionSignature *sig,
                SignatureToken ***tokens,
                int *num_tokens,
                int *raw_size,
                SignatureToken **longest_token,
                int *longest_offset,
                char **error)
{
#endif

Signature::Signature(const SignatureSpec *spec)
{
    Initialize(spec);
}

void
Signature::Initialize(const SignatureSpec *spec)
{
    m_tokens.clear();

    OIStringStream iss(spec->moduleName);
    iss.good();







#if 0
    const char *p;
    int sig_len, max_tokens, token_index, buf_pos, i, offset;
    SignatureToken **ret_tokens, *cur_token;

    ret_tokens = NULL;

    // Count the number of '?' characters in order to get
    // an idea of the maximum number of tokens we're
    // going to parse, and find the string length while
    // we're at it.
    sig_len = max_tokens = 0;
    for (p = sig->signature; *p != '\0'; p++)
    {
        sig_len++;

        if (*p == '?')
            max_tokens++;
    }

    if (max_tokens == 0)
    {
        max_tokens++;
    }
    else
    {
        if (max_tokens % 2 != 0)
        {
            *error = ospy_strdup("syntax error: unbalanced questionmarks");
            goto ERR_OUT;
        }

        max_tokens = (max_tokens / 2) + 1;
    }

    /* Make it NULL-terminated. */
    max_tokens++;

    /* Allocate an array of token-pointers. */
    ret_tokens = (SignatureToken **) ospy_malloc(max_tokens * sizeof(SignatureToken *));
    memset(ret_tokens, 0, max_tokens * sizeof(SignatureToken *));
    cur_token = NULL;
    token_index = 0;

    /* Start parsing. */
    p = sig->signature;
    while (TRUE)
    {
        unsigned int tmp;
        int ret, ignores;

        ret = sscanf(p, " %02x", &tmp);
        if (ret != 1)
        {
            break;
        }

        if (cur_token == NULL)
        {
            cur_token = signature_token_new(TOKEN_TYPE_LITERAL, sig_len + 1);
            buf_pos = 0;
        }

        cur_token->data[buf_pos] = tmp;
        cur_token->length++;
        buf_pos++;

        if (!find_next_token(&p, &ignores, error))
        {
            goto ERR_OUT;
        }

        if (ignores > 0)
        {
            ret_tokens[token_index++] = cur_token;

            cur_token = signature_token_new(TOKEN_TYPE_IGNORE, 0);
            cur_token->length = ignores;
            ret_tokens[token_index++] = cur_token;

            cur_token = NULL;
        }

        if (p == NULL)
        {
            break;
        }
    }

    if (cur_token != NULL)
    {
        ret_tokens[token_index++] = cur_token;
    }

    *tokens = ret_tokens;
    *num_tokens = token_index;

    /* Find the longest token and its offset. */
    offset = 0;
    *longest_token = ret_tokens[0];
    *longest_offset = 0;
    for (i = 0; i < *num_tokens; i++)
    {
        cur_token = ret_tokens[i];

        if (cur_token->type == TOKEN_TYPE_LITERAL)
        {
            if (cur_token->length > (*longest_token)->length)
            {
                *longest_token = cur_token;
                *longest_offset = offset;
            }
        }

        offset += cur_token->length;
    }

    *raw_size = offset;
    
    return TRUE;

ERR_OUT:
    if (ret_tokens != NULL)
    {
        signature_token_list_free(ret_tokens);
    }

    return FALSE;
#endif
}

unsigned int
SignatureMatcher::FindInRange(const SignatureSpec *spec, void *base, unsigned int size, void **firstMatch)
{
    return 0;

#if 0
    int numMatches = 0;
    unsigned char *p, *match;
    SignatureToken **tokens = NULL, *longestToken;
    int numTokens, rawSize, longestOffset;

    ParseSignature(spec, &tokens, &numTokens, &rawSize, &longestToken, &longestOffset);

    *firstMatch = NULL;
    numMatches = 0;

    for (p = (unsigned char *) base; p <= (unsigned char *) base + size - rawSize; p++)
    {
        if (memcmp(p, longestToken->data, longestToken->length) == 0)
        {
            BOOL matched = scan_against_all_tokens(p - longestOffset, tokens);
            if (matched == TRUE)
            {
                match = p - longestOffset;

				if (*firstMatch == NULL)
					*firstMatch = match;

				numMatches++;

                /* Skip ahead. */
                p = p - longestOffset + raw_size;
            }
        }
    }

	result = TRUE;

DONE:
    if (tokens != NULL)
    {
        signature_token_list_free(tokens);
    }

    return result;
#endif
}

#if 0
static BOOL
find_next_token(const char **input, int *num_ignores, char **error)
{
    const char *p = *input;
    int chars_skipped = 0, qms_skipped = 0;
    *num_ignores = 0;

    for (; *p != '\0'; p++)
    {
        if ((*p >= 48 && *p <= 57) ||
            (*p >= 65 && *p <= 90) ||
            (*p >= 97 && *p <= 122))
        {
            chars_skipped++;

            if (chars_skipped == 3)
            {
                goto DONE;
            }
        }
        else if (*p == '?')
        {
            qms_skipped++;
        }
    }

    if (*p == '\0')
        p = NULL;

DONE:
    *input = p;
    *num_ignores = qms_skipped / 2;
    return TRUE;
}

static SignatureToken *
signature_token_new (SignatureTokenType type, int buffer_size)
{
    SignatureToken *token;

    token = (SignatureToken *) ospy_malloc(sizeof(SignatureToken));
    token->type = type;
    token->length = 0;

    if (buffer_size > 0)
        token->data = (unsigned char *) ospy_malloc(buffer_size);
    else
        token->data = NULL;

    return token;
}

static void
signature_token_free (SignatureToken *token)
{
    if (token->data != NULL)
    {
        ospy_free(token->data);
    }

    ospy_free(token);
}

static void
signature_token_list_free (SignatureToken **tokens)
{
    int i;

    for (i = 0; i; i++)
    {
        SignatureToken *cur_token = tokens[i];

        if (cur_token != NULL)
        {
            signature_token_free(cur_token);
        }
        else
        {
            break;
        }
    }

    ospy_free(tokens);
}

static void
signature_token_print (SignatureToken *token)
{
    fprintf(stderr, "type=%s, length=%d",
        (token->type == TOKEN_TYPE_LITERAL) ? "TOKEN_TYPE_LITERAL" : "TOKEN_TYPE_IGNORE ",
        token->length);

    if (token->data != NULL)
    {
        int j;

        fprintf(stderr, ", data=[");
        for (j = 0; j < token->length; j++)
        {
            fprintf(stderr, " %02hhx", token->data[j]);
        }

        fprintf(stderr, "]");
    }

    fprintf(stderr, "\n");
}

static BOOL
scan_against_all_tokens(unsigned char *p, SignatureToken **tokens)
{
    SignatureToken **cur_token;

    for (cur_token = tokens; *cur_token != NULL; cur_token++)
    {
        if ((*cur_token)->type == TOKEN_TYPE_LITERAL)
        {
            if (memcmp(p, (*cur_token)->data, (*cur_token)->length) != 0)
                return FALSE;
        }

        p += (*cur_token)->length;
    }

    return TRUE;
}

BOOL
find_unique_signature_in_module(const FunctionSignature *sig, const char *module_name, LPVOID *address, char **error)
{
    BOOL result = FALSE;
	unsigned char *base;
    DWORD size, num_matches;

	if (!get_module_base_and_size(module_name, (LPVOID *) &base, &size, error))
    {
        goto DONE;
    }

	if (!find_signature_in_range(sig, base, size, address, &num_matches, error))
		goto DONE;

    if (num_matches == 0)
    {
        *error = ospy_strdup("No matches found");
        goto DONE;
    }
    else if (num_matches > 1)
    {
        *error = ospy_strdup("More than one match found");
        goto DONE;
    }

	result = true;

DONE:
	return result;
}

BOOL
find_unique_signature(const FunctionSignature *sig, LPVOID *address, char **error)
{
    return find_unique_signature_in_module(sig, sig->module_name, address, error);
}
#endif

} // namespace TrampoLib