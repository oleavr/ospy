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

namespace InterceptPP {

HookManager::HookManager()
{
}

HookManager::~HookManager()
{
    FunctionSpecMap::iterator fsIter;
    for (fsIter = m_funcSpecs.begin(); fsIter != m_funcSpecs.end(); fsIter++)
    {
        delete fsIter->second;
    }

    DllFunctionList::iterator dfIter;
    for (dfIter = m_dllFunctions.begin(); dfIter != m_dllFunctions.end(); dfIter++)
    {
        delete *dfIter;
    }

    DllModuleMap::iterator dmIter;
    for (dmIter = m_dllModules.begin(); dmIter != m_dllModules.end(); dmIter++)
    {
        delete dmIter->second;
    }
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
HookManager::LoadDefinitions(const OString &path)
{
    CoInitialize(NULL);

    try
    {
        MSXML2::IXMLDOMDocument3Ptr doc;
        HRESULT hr = doc.CreateInstance(__uuidof(MSXML2::DOMDocument60));
        if (FAILED(hr))
            throw ParserError("CreateInstance failed");

        doc->async = VARIANT_FALSE;

        if(doc->load(path.c_str()) != VARIANT_TRUE)
            throw ParserError("IXMLDOMDocument::load() failed");

        Logger *logger = GetLogger();

        MSXML2::IXMLDOMNodePtr node;
        MSXML2::IXMLDOMNodeListPtr nodeList;
        
        nodeList = doc->selectNodes("/HookManager/Specs/Functions/*");
        for (int i = 0; i < nodeList->length; i++)
        {
            node = nodeList->item[i];

            OString nodeName = node->nodeName;
            if (nodeName == "Function")
            {
                ParseFunctionSpecNode(node);
            }
            else
            {
                logger->LogWarning("Unknown element '%s'", nodeName.c_str());
            }
        }
        nodeList.Release();
        nodeList = NULL;

        nodeList = doc->selectNodes("/HookManager/Hooks/*");
        for (int i = 0; i < nodeList->length; i++)
        {
            node = nodeList->item[i];

            OString nodeName = node->nodeName;
            if (nodeName == "DllModule")
            {
                ParseDllModuleNode(node);
            }
            else
            {
                logger->LogWarning("Unknown hook element '%s'", nodeName.c_str());
            }
        }
        nodeList.Release();
        nodeList = NULL;

        doc.Release();
    }
    catch (_com_error &e)
    {
        throw ParserError(e.ErrorMessage());
    }
}

void
HookManager::ParseFunctionSpecNode(MSXML2::IXMLDOMNodePtr &funcSpecNode)
{
    OString id, name;
    CallingConvention conv = CALLING_CONV_UNKNOWN;
    int argsSize = -1;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = funcSpecNode->attributes;

    MSXML2::IXMLDOMNodePtr attrNode;
    while ((attrNode = attrs->nextNode()) != NULL)
    {
        OString attrName = static_cast<OString>(attrNode->nodeName);

        if (attrName == "Id")
        {
            id = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "Name")
        {
            name = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "CallingConvention")
        {
            OString convName;
            
            convName = static_cast<bstr_t>(attrNode->nodeTypedValue);
            if (convName == "stdcall")
            {
                conv = CALLING_CONV_STDCALL;
            }
            else if (convName == "thiscall")
            {
                conv = CALLING_CONV_THISCALL;
            }
            else if (convName == "cdecl")
            {
                conv = CALLING_CONV_CDECL;
            }
            else
            {
                GetLogger()->LogWarning("Unknown CallingConvention '%s'", convName.c_str());
                continue;
            }
        }
        else if (attrName == "ArgsSize")
        {
            argsSize = attrNode->nodeTypedValue;
        }
        else
        {
            GetLogger()->LogWarning("Unknown FunctionSpec attribute '%s'", attrName.c_str());
            continue;
        }
    }

    attrs.Release();
    
    if (name.size() > 0)
    {
        if (id.size() == 0)
            id = name;

        FunctionSpec *funcSpec = new FunctionSpec(name, conv, argsSize);
        m_funcSpecs[id] = funcSpec;

        MSXML2::IXMLDOMNodePtr node;
        MSXML2::IXMLDOMNodeListPtr nodeList;
        
        nodeList = funcSpecNode->childNodes;
        for (int i = 0; i < nodeList->length; i++)
        {
            node = nodeList->item[i];

            OString nodeName = node->nodeName;
            if (nodeName == "Arguments")
            {
                MSXML2::IXMLDOMNodePtr subNode;
                MSXML2::IXMLDOMNodeListPtr subNodeList;

                subNodeList = node->childNodes;
                int argIndex = 0;
                for (int i = 0; i < subNodeList->length; i++)
                {
                    subNode = subNodeList->item[i];

                    OString subNodeName = subNode->nodeName;
                    if (subNodeName == "Argument")
                    {
                        ParseFunctionSpecArgumentNode(funcSpec, subNode, argIndex);
                        argIndex++;
                    }
                    else
                    {
                        GetLogger()->LogWarning("Unknown Arguments subelement '%s'", subNodeName.c_str());
                    }
                }
            }
            else
            {
                GetLogger()->LogWarning("Unknown FunctionSpec subelement '%s'", nodeName.c_str());
            }
        }
        nodeList.Release();
        nodeList = NULL;
    }
    else
    {
        GetLogger()->LogError("Name not specified for FunctionSpec");
    }
}

void
HookManager::ParseFunctionSpecArgumentNode(FunctionSpec *funcSpec, MSXML2::IXMLDOMNodePtr &argNode, int argIndex)
{
    OString argName;
    ArgumentDirection argDir = ARG_DIR_UNKNOWN;
    OString argType;
    OMap<OString, OString>::Type argTypeOpts;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = argNode->attributes;

    MSXML2::IXMLDOMNodePtr attrNode;
    while ((attrNode = attrs->nextNode()) != NULL)
    {
        OString attrName = static_cast<OString>(attrNode->nodeName);

        if (attrName == "Name")
        {
            argName = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "Direction")
        {
            OString dirStr = static_cast<bstr_t>(attrNode->nodeTypedValue);

            if (dirStr == "in")
                argDir = ARG_DIR_IN;
            else if (dirStr == "out")
                argDir = ARG_DIR_OUT;
            else if (dirStr == "in/out")
                argDir = static_cast<ArgumentDirection> (ARG_DIR_IN | ARG_DIR_OUT);
            else
            {
                GetLogger()->LogWarning("Unknown direction '%s'", dirStr.c_str());
            }
        }
        else if (attrName == "Type")
        {
            argType = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else
        {
            argTypeOpts[attrName] = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
    }

    attrs.Release();

    if (argName.size() == 0)
    {
        OOStringStream ss;
        ss << "Arg" << (argIndex + 1);
        argName = ss.str();
    }

    if (argDir == ARG_DIR_UNKNOWN)
    {
        GetLogger()->LogError("Argument direction not specified");
        return;
    }
    else if (argType.size() == 0)
    {
        GetLogger()->LogError("Argument type not specified");
        return;
    }


}

void
HookManager::ParseDllModuleNode(MSXML2::IXMLDOMNodePtr &dllModNode)
{
    DllModule *dllMod = NULL;
    OICString name;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = dllModNode->attributes;

    MSXML2::IXMLDOMNodePtr attrNode;
    while ((attrNode = attrs->nextNode()) != NULL)
    {
        OString attrName = static_cast<OString>(attrNode->nodeName);

        if (attrName == "Name")
        {
            name = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else
        {
            GetLogger()->LogWarning("Unknown DllModule attribute '%s'", attrName.c_str());
            continue;
        }
    }

    attrs.Release();

    if (name.size() > 0)
    {
        dllMod = new DllModule(name.c_str());
        m_dllModules[name] = dllMod;
    }
    else
    {
        GetLogger()->LogError("Name not specified for DllModule");
        return;
    }

    MSXML2::IXMLDOMNodePtr node;
    MSXML2::IXMLDOMNodeListPtr nodeList;
    
    nodeList = dllModNode->childNodes;
    for (int i = 0; i < nodeList->length; i++)
    {
        node = nodeList->item[i];

        OString nodeName = node->nodeName;
        if (nodeName == "Function")
        {
            ParseDllFunctionNode(dllMod, node);
        }
        else
        {
            GetLogger()->LogWarning("Unknown DllModule subelement '%s'", nodeName.c_str());
        }
    }
    nodeList.Release();
    nodeList = NULL;
}

void
HookManager::ParseDllFunctionNode(DllModule *dllMod, MSXML2::IXMLDOMNodePtr &dllFuncNode)
{
    OString specId;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = dllFuncNode->attributes;

    MSXML2::IXMLDOMNodePtr attrNode;
    while ((attrNode = attrs->nextNode()) != NULL)
    {
        OString attrName = static_cast<OString>(attrNode->nodeName);

        if (attrName == "SpecId")
        {
            specId = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else
        {
            GetLogger()->LogWarning("Unknown DllFunction attribute '%s'", attrName.c_str());
            continue;
        }
    }

    attrs.Release();

    if (specId.size() > 0)
    {
        if (m_funcSpecs.find(specId) != m_funcSpecs.end())
        {
            DllFunction *dllFunc = new DllFunction(dllMod, m_funcSpecs[specId]);
            m_dllFunctions.push_back(dllFunc);
            dllFunc->Hook();
        }
        else
        {
            GetLogger()->LogError("SpecId '%s' not found", specId.c_str());
        }
    }
    else
    {
        GetLogger()->LogError("SpecId not specified for DllFunction");
    }
}

} // namespace InterceptPP