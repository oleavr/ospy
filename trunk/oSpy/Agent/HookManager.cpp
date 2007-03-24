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
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#include "stdafx.h"
#include "HookManager.h"
#import <msxml4.dll>

using namespace MSXML2;

HookManager::HookManager()
{
}

HookManager *
HookManager::Instance()
{
    static HookManager *mgr = NULL;

    if (mgr == NULL)
        mgr = new HookManager();

    return mgr;
}

void
HookManager::LoadDefinitions()
{
    CoInitialize(NULL);

    IXMLDOMDocument2Ptr doc;
    HRESULT hr = doc.CreateInstance(__uuidof(DOMDocument40));
    if (FAILED(hr))
        throw runtime_error("CreateInstance failed");

    doc->async = VARIANT_FALSE;

    if(doc->load("c:\\hooks.xml") != VARIANT_TRUE)
        throw runtime_error("failed to load specs.xml");

    Logging::Logger *logger = GetLogger();

    IXMLDOMNodePtr node;
    IXMLDOMNodeListPtr nodeList = doc->selectNodes("/Hooks/Specs/*");
    for (int i = 0; i < nodeList->length; i++)
    {
        node = nodeList->item[i];


        printf("Node (%d), <%s>:\n\t%s\n", 
        i, (LPCSTR)pNode->nodeName, (LPCSTR)pnl->item[i]->xml);
    }
    nodeList.Release();

    doc.Release();
}
