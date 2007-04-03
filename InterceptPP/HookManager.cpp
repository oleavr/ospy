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
#include "Util.h"

#pragma warning( disable : 4311 4312 )

namespace InterceptPP {

HookManager::HookManager()
{
}

HookManager::~HookManager()
{
    VTableList::iterator vtIter;
    for (vtIter = m_vtables.begin(); vtIter != m_vtables.end(); vtIter++)
    {
        delete *vtIter;
    }

    FunctionList::iterator funcIter;
    for (funcIter = m_functions.begin(); funcIter != m_functions.end(); funcIter++)
    {
        delete *funcIter;
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

    FunctionSpecMap::iterator fsIter;
    for (fsIter = m_funcSpecs.begin(); fsIter != m_funcSpecs.end(); fsIter++)
    {
        delete fsIter->second;
    }

    VTableSpecMap::iterator vtsIter;
    for (vtsIter = m_vtableSpecs.begin(); vtsIter != m_vtableSpecs.end(); vtsIter++)
    {
        delete vtsIter->second;
    }

    SignatureMap::iterator sigIter;
    for (sigIter = m_signatures.begin(); sigIter != m_signatures.end(); sigIter++)
    {
        delete sigIter->second;
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
HookManager::LoadDefinitions(const OWString &path)
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

        MSXML2::IXMLDOMNodeListPtr nodeList;
        MSXML2::IXMLDOMNodePtr node;

        // TODO: refactor this mess

        {
            nodeList = doc->selectNodes("/HookManager/Types/*");
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseTypeNode(nodeList->item[i]);
            }
            nodeList.Release();
        }

        {
            nodeList = doc->selectNodes("/HookManager/Specs/Functions/Function");
            for (int i = 0; i < nodeList->length; i++)
            {
                OString id;

                FunctionSpec *funcSpec = ParseFunctionSpecNode(nodeList->item[i], id);
                if (funcSpec != NULL)
                {
                    m_funcSpecs[id] = funcSpec;
                }
            }
            nodeList.Release();
        }

        {
            nodeList = doc->selectNodes("/HookManager/Specs/VTables/VTable");
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseVTableSpecNode(nodeList->item[i]);
            }
            nodeList.Release();
        }

        {
            nodeList = doc->selectNodes("/HookManager/Signatures/Signature");
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseSignatureNode(nodeList->item[i]);
            }
            nodeList.Release();
        }

        {
            nodeList = doc->selectNodes("/HookManager/Hooks/DllModule");
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseDllModuleNode(nodeList->item[i]);
            }
            nodeList.Release();
        }

        const OString &processName = Util::Instance()->GetProcessName();

        {
            OOStringStream ss;
            ss << "/HookManager/Hooks/Functions[@ProcessName = translate('";
            ss << processName;
            ss << "', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')]/Function";

            nodeList = doc->selectNodes(ss.str().c_str());
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseFunctionNode(processName, nodeList->item[i]);
            }
            nodeList.Release();
        }

        {
            OOStringStream ss;
            ss << "/HookManager/Hooks/VTables[@ProcessName = translate('";
            ss << processName;
            ss << "', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')]/VTable";

            nodeList = doc->selectNodes(ss.str().c_str());
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseVTableNode(processName, nodeList->item[i]);
            }
            nodeList.Release();
        }

        doc.Release();
    }
    catch (_com_error &e)
    {
        throw ParserError(e.ErrorMessage());
    }
}

void
HookManager::ParseTypeNode(MSXML2::IXMLDOMNodePtr &typeNode)
{
    OString typeName = typeNode->nodeName;
    if (typeName == "Structure")
    {
        ParseStructureNode(typeNode);
    }
    else if (typeName == "Enumeration")
    {
        ParseEnumerationNode(typeNode);
    }
    else
    {
        GetLogger()->LogWarning("Unknown type '%s'", typeName.c_str());
    }
}

void
HookManager::ParseStructureNode(MSXML2::IXMLDOMNodePtr &structNode)
{
    OString name;
    MSXML2::IXMLDOMNodePtr attr = structNode->attributes->getNamedItem("Name");
    if (attr == NULL)
    {
        GetLogger()->LogError("Name not specified for Structure");
        return;
    }

    name = static_cast<bstr_t>(attr->nodeTypedValue);
    if (name.size() == 0)
    {
        GetLogger()->LogError("Empty name specified for Structure");
        return;
    }

    Marshaller::Structure *structTpl = new Marshaller::Structure(name.c_str(), NULL);

    MSXML2::IXMLDOMNodeListPtr nodeList;
    
    bool allGood = true;
    nodeList = structNode->childNodes;
    for (int i = 0; i < nodeList->length && allGood; i++)
    {
        MSXML2::IXMLDOMNodePtr node = nodeList->item[i];

        OString nodeName = node->nodeName;
        if (nodeName == "Field")
        {
            OString fieldName, fieldType;
            int fieldOffset;
            PropertyList typeProps;

            if (ParseStructureFieldNode(node, fieldName, fieldOffset, fieldType, typeProps))
            {
                BaseMarshaller *marshaller = Marshaller::Factory::Instance()->CreateMarshaller(fieldType);

                for (PropertyList::const_iterator iter = typeProps.begin(); iter != typeProps.end(); iter++)
                {
                    if (!marshaller->SetProperty(iter->first, iter->second))
                    {
                        GetLogger()->LogWarning("Failed to set property '%s' to '%s' on Marshaller::%s",
                            iter->first.c_str(), iter->second.c_str(), fieldType.c_str());
                    }
                }

                structTpl->AddField(new Marshaller::StructureField(fieldName, fieldOffset, marshaller));
            }
            else
            {
                allGood = false;
            }
        }
        else
        {
            GetLogger()->LogWarning("Unknown Structure subelement '%s'", nodeName.c_str());
        }
    }
    nodeList.Release();

    if (allGood && structTpl->GetFieldCount() == 0)
    {
        GetLogger()->LogError("No fields defined for structure '%s'", name.c_str());
        allGood = false;
    }

    if (!allGood)
    {
        delete structTpl;
        return;
    }

    TypeBuilder::Instance()->AddType(structTpl);
    TypeBuilder::Instance()->AddType(new Marshaller::Pointer(structTpl, name + "Ptr"));
}

bool
HookManager::ParseStructureFieldNode(MSXML2::IXMLDOMNodePtr &fieldNode,
                                     OString &name, int &offset, OString &typeName,
                                     PropertyList &typeProps)
{
    offset = -1;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = fieldNode->attributes;
    MSXML2::IXMLDOMNodePtr attrNode;

    while ((attrNode = attrs->nextNode()) != NULL)
    {
        OString attrName = static_cast<OString>(attrNode->nodeName);

        if (attrName == "Name")
        {
            name = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "Offset")
        {
            OString offsetStr = static_cast<bstr_t>(attrNode->nodeTypedValue);

            // FIXME: there's gotta be a more sensible way to do this
            char *endPtr = NULL;
            offset = strtoul(offsetStr.c_str(), &endPtr, 0);
            if (endPtr == offsetStr.c_str() || offset < 0)
            {
                GetLogger()->LogError("Invalid offset specified for Structure field");
                return false;
            }
        }
        else if (attrName == "Type")
        {
            typeName = static_cast<bstr_t>(attrNode->nodeTypedValue);

            if (!Marshaller::Factory::Instance()->HasMarshaller(typeName))
            {
                GetLogger()->LogError("Unknown type '%s' specified for Structure field", typeName.c_str());
                return false;
            }
        }
        else
        {
            typeProps.push_back(pair<OString, OString>(attrName, OString(static_cast<bstr_t>(attrNode->nodeTypedValue))));
        }
    }

    if (name.size() == 0)
    {
        GetLogger()->LogError("Name blank or not specified for Structure field");
        return false;
    }
    else if (offset == -1)
    {
        GetLogger()->LogError("Offset not specified for Structure field");
        return false;
    }
    else if (typeName.size() == 0)
    {
        GetLogger()->LogError("Type name not specified for Structure field");
        return false;
    }

    return true;
}

void
HookManager::ParseEnumerationNode(MSXML2::IXMLDOMNodePtr &enumNode)
{
    OString name;
    MSXML2::IXMLDOMNodePtr attr = enumNode->attributes->getNamedItem("Name");
    if (attr == NULL)
    {
        GetLogger()->LogError("Name not specified for Enumeration");
        return;
    }

    name = static_cast<bstr_t>(attr->nodeTypedValue);
    if (name.size() == 0)
    {
        GetLogger()->LogError("Empty name specified for Enumeration");
        return;
    }

    Marshaller::Enumeration *enumTpl = new Marshaller::Enumeration(name.c_str(), NULL);

    MSXML2::IXMLDOMNodeListPtr nodeList;

    nodeList = enumNode->childNodes;
    for (int i = 0; i < nodeList->length; i++)
    {
        MSXML2::IXMLDOMNodePtr node = nodeList->item[i];

        OString nodeName = node->nodeName;
        if (nodeName == "Member")
        {
            OString memberName;
            DWORD memberValue;

            if (ParseEnumerationMemberNode(node, memberName, memberValue))
            {
                enumTpl->AddMember(memberName, memberValue);
            }
        }
        else
        {
            GetLogger()->LogWarning("Unknown Enumeration subelement '%s'", nodeName.c_str());
        }
    }
    nodeList.Release();

    if (enumTpl->GetMemberCount() > 0)
    {
        TypeBuilder::Instance()->AddType(enumTpl);
    }
    else
    {
        GetLogger()->LogError("No valid members defined for Enumeration '%s'", name.c_str());
        delete enumTpl;
    }
}

bool
HookManager::ParseEnumerationMemberNode(MSXML2::IXMLDOMNodePtr &enumMemberNode, OString &memberName, DWORD &memberValue)
{
    MSXML2::IXMLDOMNamedNodeMapPtr attrs = enumMemberNode->attributes;
    MSXML2::IXMLDOMNodePtr attr;

    attr = attrs->getNamedItem("Name");
    if (attr == NULL)
    {
        GetLogger()->LogError("Name not specified for Enumeration member");
        return false;
    }
    memberName = static_cast<bstr_t>(attr->nodeTypedValue);

    attr = attrs->getNamedItem("Value");
    if (attr == NULL)
    {
        GetLogger()->LogError("Offset not specified for Enumeration member");
        return NULL;
    }
    OString valStr = static_cast<bstr_t>(attr->nodeTypedValue);

    // FIXME: there's gotta be a more sensible way to do this
    char *endPtr = NULL;
    memberValue = strtoul(valStr.c_str(), &endPtr, 0);
    if (endPtr == valStr.c_str())
    {
        GetLogger()->LogError("Invalid offset specified for Enumeration member");
        return false;
    }

    return true;
}

FunctionSpec *
HookManager::ParseFunctionSpecNode(MSXML2::IXMLDOMNodePtr &funcSpecNode, OString &id, bool nameRequired, bool ignoreUnknown)
{
    FunctionSpec *funcSpec = NULL;
    OString name;
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
            if (!ignoreUnknown)
            {
                GetLogger()->LogWarning("Unknown FunctionSpec attribute '%s'", attrName.c_str());
            }
        }
    }

    attrs.Release();
    
    if (name.size() > 0 || !nameRequired)
    {
        if (id.size() == 0)
            id = name;

        funcSpec = new FunctionSpec(name, conv, argsSize);

        MSXML2::IXMLDOMNodePtr node;
        MSXML2::IXMLDOMNodeListPtr nodeList;
        
        nodeList = funcSpecNode->childNodes;
        for (int i = 0; i < nodeList->length; i++)
        {
            node = nodeList->item[i];

            OString nodeName = node->nodeName;
            if (nodeName == "Arguments")
            {
                ArgumentListSpec *argList = new ArgumentListSpec();
                bool argsOk = true;

                MSXML2::IXMLDOMNodePtr subNode;
                MSXML2::IXMLDOMNodeListPtr subNodeList;

                subNodeList = node->childNodes;
                int argIndex = 0;
                for (int i = 0; i < subNodeList->length && argsOk; i++)
                {
                    subNode = subNodeList->item[i];

                    OString subNodeName = subNode->nodeName;
                    if (subNodeName == "Argument")
                    {
                        ArgumentSpec *arg = ParseFunctionSpecArgumentNode(funcSpec, subNode, argIndex);
                        if (arg != NULL)
                        {
                            argList->AddArgument(arg);
                        }
                        else
                        {
                            argsOk = false;
                        }

                        argIndex++;
                    }
                    else
                    {
                        GetLogger()->LogWarning("Unknown Arguments subelement '%s'", subNodeName.c_str());
                    }
                }

                if (argsOk)
                {
                    funcSpec->SetArguments(argList);
                }
                else
                {
                    delete argList;
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
        GetLogger()->LogError("Name is blank or not specified for FunctionSpec");
    }

    return funcSpec;
}

ArgumentSpec *
HookManager::ParseFunctionSpecArgumentNode(FunctionSpec *funcSpec, MSXML2::IXMLDOMNodePtr &argNode, int argIndex)
{
    OString argName;
    ArgumentDirection argDir = ARG_DIR_UNKNOWN;
    OString argType;
    PropertyList argTypeProps;

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
            argTypeProps.push_back(pair<OString, OString>(attrName, OString(static_cast<bstr_t>(attrNode->nodeTypedValue))));
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
        return NULL;
    }
    else if (argType.size() == 0)
    {
        GetLogger()->LogError("Argument type is blank or not specified");
        return NULL;
    }

    BaseMarshaller *marshaller = Marshaller::Factory::Instance()->CreateMarshaller(argType);
    if (marshaller == NULL)
    {
        GetLogger()->LogError("Argument type '%s' is unknown", argType.c_str());
        return NULL;
    }

    for (PropertyList::const_iterator iter = argTypeProps.begin(); iter != argTypeProps.end(); iter++)
    {
        if (!marshaller->SetProperty(iter->first, iter->second))
        {
            GetLogger()->LogWarning("Failed to set property '%s' to '%s' on Marshaller::%s",
                iter->first.c_str(), iter->second.c_str(), argType.c_str());
        }
    }

    return new ArgumentSpec(argName, argDir, marshaller);
}

void
HookManager::ParseVTableSpecNode(MSXML2::IXMLDOMNodePtr &vtSpecNode)
{
    OString id;
    int methodCount = -1;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = vtSpecNode->attributes;

    MSXML2::IXMLDOMNodePtr attrNode;
    while ((attrNode = attrs->nextNode()) != NULL)
    {
        OString attrName = static_cast<OString>(attrNode->nodeName);

        if (attrName == "Id")
        {
            id = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "MethodCount")
        {
            methodCount = attrNode->nodeTypedValue;
        }
        else
        {
            GetLogger()->LogWarning("Unknown VTableSpec attribute '%s'", attrName.c_str());
            continue;
        }
    }

    attrs.Release();
    attrs = NULL;

    if (id.size() == 0)
    {
        GetLogger()->LogError("VTableSpec Id is blank or not specified");
        return;
    }
    else if (methodCount <= 0)
    {
        GetLogger()->LogError("VTableSpec MethodCount is invalid or not specified");
        return;
    }

    VTableSpec *vtSpec = new VTableSpec(id, methodCount);
    m_vtableSpecs[id] = vtSpec;

    MSXML2::IXMLDOMNodeListPtr nodeList;

    nodeList = vtSpecNode->childNodes;
    for (int i = 0; i < nodeList->length; i++)
    {
        MSXML2::IXMLDOMNodePtr node = nodeList->item[i];

        OString nodeName = node->nodeName;
        if (nodeName == "Method")
        {
            MSXML2::IXMLDOMNamedNodeMapPtr attrs = node->attributes;
            MSXML2::IXMLDOMNodePtr indexAttr = attrs->getNamedItem("Index");

            int index = -1;
            if (indexAttr != NULL)
            {
                index = indexAttr->nodeTypedValue;
            }

            if (index >= 0 && index < static_cast<int>(vtSpec->GetMethodCount()))
            {
                OString id;
                FunctionSpec *funcSpec = ParseFunctionSpecNode(node, id, false, true);
                if (funcSpec != NULL)
                {
                    (*vtSpec)[index].StealFrom(funcSpec);
                    delete funcSpec;
                }
            }
            else
            {
                GetLogger()->LogWarning("Required VMethodSpec attribute 'Index' is invalid or not specified");
            }

            attrs.Release();
        }
        else
        {
            GetLogger()->LogWarning("Unknown VTableSpec subelement '%s'", nodeName.c_str());
        }
    }
    nodeList.Release();
}

static OString
FilterString(const OString &str)
{
    OString result;

    for (unsigned int i = 0; i < str.size(); i++)
    {
        char c = str[i];
        if (isalnum(c) || c == ' ')
        {
            result += c;
        }
    }

    return result;
}

static void
TrimString(OString &str)
{
    string::size_type pos = str.find_last_not_of(' ');
    if (pos != string::npos)
    {
        str.erase(pos + 1);
        pos = str.find_first_not_of(' ');
        if (pos != string::npos)
            str.erase(0, pos);
    }
    else str.erase(str.begin(), str.end());
}

void
HookManager::ParseSignatureNode(MSXML2::IXMLDOMNodePtr &sigNode)
{
    OString name;
    MSXML2::IXMLDOMNodePtr attr = sigNode->attributes->getNamedItem("Name");
    if (attr == NULL)
    {
        GetLogger()->LogError("Name not specified for Signature");
        return;
    }

    name = static_cast<bstr_t>(attr->nodeTypedValue);
    if (name.size() == 0)
    {
        GetLogger()->LogError("Empty name specified for Signature");
        return;
    }

    OOStringStream ss;
    MSXML2::IXMLDOMNodeListPtr nodeList = sigNode->childNodes;
    for (int i = 0; i < nodeList->length; i++)
    {
        MSXML2::IXMLDOMNodePtr node = nodeList->item[i];

        if (node->nodeType == NODE_TEXT)
        {
            OString chunk = static_cast<bstr_t>(node->nodeTypedValue);
            chunk = FilterString(chunk);
            TrimString(chunk);

            if (i)
                ss << ' ';
            ss << chunk;
        }
    }

    OString sigStr = ss.str();
    if (sigStr.size() == 0)
    {
        GetLogger()->LogError("Signature definition '%s' is empty", name.c_str());
        return;
    }

    try
    {
        m_signatures[name] = new Signature(sigStr);
    }
    catch (Error &e)
    {
        GetLogger()->LogError("Signature definition '%s' is invalid: %s", name.c_str(), e.what());
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

void
HookManager::ParseFunctionNode(const OString &processName, MSXML2::IXMLDOMNodePtr &funcNode)
{
    OString specId, sigId;
    int sigOffset = 0;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = funcNode->attributes;
    MSXML2::IXMLDOMNodePtr attr;

    attr = attrs->getNamedItem("SpecId");
    if (attr == NULL)
    {
        GetLogger()->LogError("Function spec id not specified for Function");
        return;
    }
    specId = static_cast<bstr_t>(attr->nodeTypedValue);

    if (m_funcSpecs.find(specId) == m_funcSpecs.end())
    {
        GetLogger()->LogError("Invalid function spec id '%s' specified for Function", specId.c_str());
        return;
    }

    attr = attrs->getNamedItem("SigId");
    if (attr == NULL)
    {
        GetLogger()->LogError("Signature id not specified for Function");
        return;
    }
    sigId = static_cast<bstr_t>(attr->nodeTypedValue);

    SignatureMap::const_iterator iter = m_signatures.find(sigId);
    if (iter == m_signatures.end())
    {
        GetLogger()->LogError("Invalid signature id '%s' specified for Function", sigId.c_str());
        return;
    }

    const Signature *sig = iter->second;

    attr = attrs->getNamedItem("SigOffset");
    if (attr != NULL)
    {
        OString sigOffsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

        char *endPtr = NULL;
        sigOffset = strtol(sigOffsetStr.c_str(), &endPtr, 0);
        if (endPtr == sigOffsetStr.c_str())
        {
            GetLogger()->LogError("Invalid signature offset specified for Function");
            return;
        }
    }

    void *startAddr;

    try
    {
        startAddr = SignatureMatcher::Instance()->FindUniqueInModule(*sig, processName.c_str());
    }
    catch (Error &e)
    {
        GetLogger()->LogError("Signature '%s' specified for Function not found: %s", sigId.c_str(), e.what());
        return;
    }

    DWORD offset = reinterpret_cast<DWORD>(startAddr) + sigOffset;

    GetLogger()->LogDebug("Function with signature id '%s' found at offset 0x%x",
                          sigId.c_str(), offset);

    Function *func = new Function(m_funcSpecs[specId], offset);
    m_functions.push_back(func);
    func->Hook();
}

void
HookManager::ParseVTableNode(const OString &processName, MSXML2::IXMLDOMNodePtr &vtNode)
{
    OString specId, name, offsetStr;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = vtNode->attributes;
    MSXML2::IXMLDOMNodePtr attr;

    attr = attrs->getNamedItem("SpecId");
    if (attr == NULL)
    {
        GetLogger()->LogError("SpecId not specified for VTable");
        return;
    }
    specId = static_cast<bstr_t>(attr->nodeTypedValue);

    if (m_vtableSpecs.find(specId) == m_vtableSpecs.end())
    {
        GetLogger()->LogError("Invalid SpecId specified for VTable");
        return;
    }

    attr = attrs->getNamedItem("Name");
    if (attr == NULL)
    {
        GetLogger()->LogError("Name not specified for VTable");
        return;
    }
    name = static_cast<bstr_t>(attr->nodeTypedValue);

    DWORD offset;

    attr = attrs->getNamedItem("CtorSigId");
    if (attr != NULL)
    {
        OString sigId = static_cast<bstr_t>(attr->nodeTypedValue);
        int sigOffset = 0;

        if (sigId.size() == 0)
        {
            GetLogger()->LogError("CtorSigId cannot be blank");
            return;
        }

        SignatureMap::const_iterator iter = m_signatures.find(sigId);
        if (iter == m_signatures.end())
        {
            GetLogger()->LogError("Constructor signature id '%s' not found", sigId.c_str());
            return;
        }

        const Signature *sig = iter->second;

        attr = attrs->getNamedItem("CtorSigOffset");
        if (attr != NULL)
        {
            OString sigOffsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

            char *endPtr = NULL;
            sigOffset = strtol(sigOffsetStr.c_str(), &endPtr, 0);
            if (endPtr == sigOffsetStr.c_str())
            {
                GetLogger()->LogError("Invalid CtorSigOffset specified for VTable '%s'", name.c_str());
                return;
            }
        }

        void *startAddr;

        try
        {
            startAddr = SignatureMatcher::Instance()->FindUniqueInModule(*sig, processName.c_str());
        }
        catch (Error &e)
        {
            GetLogger()->LogError("Constructor signature specified for VTable '%s' not found: %s", name.c_str(), e.what());
            return;
        }

        startAddr = static_cast<char *>(startAddr) + sigOffset;
        offset = *(static_cast<DWORD *>(startAddr));

        if (!Util::Instance()->AddressIsWithinModule(offset, processName.c_str()))
        {
            GetLogger()->LogError("SigOffset specified for VTable '%s' seems to be invalid, pointer offset=0x%p, vtable offset=0x%08x",
                                  name.c_str(), startAddr, offset);
            return;
        }

        GetLogger()->LogDebug("VTable '%s': found at offset 0x%08x", name.c_str(), offset);
    }
    else
    {
        attr = attrs->getNamedItem("Offset");
        if (attr == NULL)
        {
            GetLogger()->LogError("Neither CtorSigId nor Offset specified for VTable '%s'", name.c_str());
            return;
        }
        offsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

        // FIXME: there's gotta be a more sensible way to do this
        char *endPtr = NULL;
        offset = strtoul(offsetStr.c_str(), &endPtr, 0);
        if (endPtr == offsetStr.c_str())
        {
            GetLogger()->LogError("Invalid offset specified for VTable");
            return;
        }
    }

    VTable *vtable = new VTable(m_vtableSpecs[specId], name, offset);
    m_vtables.push_back(vtable);
    vtable->Hook();
}

TypeBuilder *
TypeBuilder::Instance()
{
    static TypeBuilder *builder = NULL;

    if (builder == NULL)
        builder = new TypeBuilder();

    return builder;
}

TypeBuilder::~TypeBuilder()
{
    for (TypeMap::iterator iter = m_types.begin(); iter != m_types.end(); iter++)
    {
        Marshaller::Factory::Instance()->UnregisterMarshaller(iter->first);
        delete iter->second;
    }
}

void
TypeBuilder::AddType(BaseMarshaller *tpl)
{
    m_types[tpl->GetName()] = tpl;
    Marshaller::Factory::Instance()->RegisterMarshaller(tpl->GetName(), BuildTypeWrapper);
}

BaseMarshaller *
TypeBuilder::BuildTypeWrapper(const OString &name)
{
    return Instance()->BuildType(name);
}

BaseMarshaller *
TypeBuilder::BuildType(const OString &name)
{
    TypeMap::iterator iter = m_types.find(name);
    if (iter == m_types.end())
        return NULL;

    return iter->second->Clone();
}

} // namespace InterceptPP