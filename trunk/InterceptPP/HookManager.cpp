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

#ifndef DEBUG
#define DEBUG 0
#endif

namespace InterceptPP {

HookManager::HookManager()
{
}

HookManager::~HookManager()
{
    Reset();
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

#if !DEBUG
    try
    {
#endif
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
            nodeList = doc->selectNodes("/hookManager/types/*");
            for (int i = 0; i < nodeList->length; i++)
            {
                node = nodeList->item[i];

                if (node->nodeType == MSXML2::NODE_ELEMENT)
                {
                    ParseTypeNode(nodeList->item[i]);
                }
            }
            nodeList.Release();
        }

        {
            nodeList = doc->selectNodes("/hookManager/specs/functions/function");
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
            nodeList = doc->selectNodes("/hookManager/specs/vtables/vtable");
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseVTableSpecNode(nodeList->item[i]);
            }
            nodeList.Release();
        }

        {
            nodeList = doc->selectNodes("/hookManager/signatures/signature");
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseSignatureNode(nodeList->item[i]);
            }
            nodeList.Release();
        }

        {
            nodeList = doc->selectNodes("/hookManager/hooks/dllModule");
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseDllModuleNode(nodeList->item[i]);
            }
            nodeList.Release();
        }

        const OString &processName = Util::Instance()->GetProcessName();

        {
            OOStringStream ss;
            ss << "/hookManager/hooks/functions[@processName = translate('";
            ss << processName;
            ss << "', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')]/function";

            nodeList = doc->selectNodes(ss.str().c_str());
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseFunctionNode(processName, nodeList->item[i]);
            }
            nodeList.Release();
        }

        {
            OOStringStream ss;
            ss << "/hookManager/hooks/vtables[@processName = translate('";
            ss << processName;
            ss << "', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')]/vtable";

            nodeList = doc->selectNodes(ss.str().c_str());
            for (int i = 0; i < nodeList->length; i++)
            {
                ParseVTableNode(processName, nodeList->item[i]);
            }
            nodeList.Release();
        }

        doc.Release();
#if !DEBUG
    }
    catch (_com_error &e)
    {
        throw ParserError(e.ErrorMessage());
    }
#endif
}

void
HookManager::Reset()
{
    VTableList::iterator vtIter;
    for (vtIter = m_vtables.begin(); vtIter != m_vtables.end(); vtIter++)
    {
        (*vtIter)->UnHook();
        delete *vtIter;
    }
    m_vtables.clear();

    FunctionList::iterator funcIter;
    for (funcIter = m_functions.begin(); funcIter != m_functions.end(); funcIter++)
    {
        (*funcIter)->UnHook();
        delete *funcIter;
    }
    m_functions.clear();

    DllFunctionList::iterator dfIter;
    for (dfIter = m_dllFunctions.begin(); dfIter != m_dllFunctions.end(); dfIter++)
    {
        (*dfIter)->UnHook();
        delete *dfIter;
    }
    m_dllFunctions.clear();

    DllModuleMap::iterator dmIter;
    for (dmIter = m_dllModules.begin(); dmIter != m_dllModules.end(); dmIter++)
    {
        delete dmIter->second;
    }
    m_dllModules.clear();

    FunctionSpecMap::iterator fsIter;
    for (fsIter = m_funcSpecs.begin(); fsIter != m_funcSpecs.end(); fsIter++)
    {
        delete fsIter->second;
    }
    m_funcSpecs.clear();

    VTableSpecMap::iterator vtsIter;
    for (vtsIter = m_vtableSpecs.begin(); vtsIter != m_vtableSpecs.end(); vtsIter++)
    {
        delete vtsIter->second;
    }
    m_vtableSpecs.clear();

    SignatureMap::iterator sigIter;
    for (sigIter = m_signatures.begin(); sigIter != m_signatures.end(); sigIter++)
    {
        delete sigIter->second;
    }
    m_signatures.clear();
}

FunctionSpec *
HookManager::GetFunctionSpecById(const OString &id)
{
	if (m_funcSpecs.find(id) != m_funcSpecs.end())
		return m_funcSpecs[id];
	else
		return NULL;
}

void
HookManager::ParseTypeNode(MSXML2::IXMLDOMNodePtr &typeNode)
{
    OString typeName = typeNode->nodeName;
    if (typeName == "structure")
    {
        ParseStructureNode(typeNode);
    }
    else if (typeName == "enumeration")
    {
        ParseEnumerationNode(typeNode);
    }
    else
    {
        GetLogger()->LogWarning("unknown type '%s'", typeName.c_str());
    }
}

void
HookManager::ParseStructureNode(MSXML2::IXMLDOMNodePtr &structNode)
{
    OString name;
    MSXML2::IXMLDOMNodePtr attr = structNode->attributes->getNamedItem("name");
    if (attr == NULL)
    {
        GetLogger()->LogError("name not specified for structure");
        return;
    }

    name = static_cast<bstr_t>(attr->nodeTypedValue);
    if (name.size() == 0)
    {
        GetLogger()->LogError("empty name specified for structure");
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
        if (nodeName == "field")
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
                        GetLogger()->LogWarning("failed to set property '%s' to '%s' on Marshaller::%s",
                            iter->first.c_str(), iter->second.c_str(), fieldType.c_str());
                    }
                }

                structTpl->AddField(new Marshaller::StructureField(fieldName, fieldOffset, marshaller));

                MSXML2::IXMLDOMNodeListPtr subNodeList = node->childNodes;
                for (int j = 0; j < subNodeList->length; j++)
                {
                    MSXML2::IXMLDOMNodePtr subNode = subNodeList->item[j];

                    OString nodeName = subNode->nodeName;
                    if (nodeName == "bindTypeProperty")
                    {
                        OString propName, srcFieldName;

                        MSXML2::IXMLDOMNodePtr attr = subNode->attributes->getNamedItem("propertyName");
                        if (attr != NULL)
                            propName = static_cast<bstr_t>(attr->nodeTypedValue);

                        if (propName.size() == 0)
                        {
                            GetLogger()->LogWarning("%s: propertyName blank or not specified for bindTypeProperty",
                                                    fieldName.c_str());
                            continue;
                        }

                        attr = subNode->attributes->getNamedItem("sourceField");
                        if (attr != NULL)
                            srcFieldName = static_cast<bstr_t>(attr->nodeTypedValue);

                        if (srcFieldName.size() == 0)
                        {
                            GetLogger()->LogWarning("%s: sourceField blank or not specified for bindTypeProperty",
                                                    fieldName.c_str());
                            continue;
                        }

                        // TODO: maybe we should check if the sourceField actually exists

                        structTpl->BindFieldTypePropertyToField(fieldName, propName, srcFieldName);
                    }
                    else if (subNode->nodeType == MSXML2::NODE_ELEMENT)
                    {
                        GetLogger()->LogWarning("%s: unknown structure field subelement '%s'",
                            fieldName.c_str(), nodeName.c_str());
                    }
                }
            }
            else
            {
                allGood = false;
            }
        }
        else if (node->nodeType == MSXML2::NODE_ELEMENT)
        {
            GetLogger()->LogWarning("unknown structure subelement '%s'", nodeName.c_str());
        }
    }
    nodeList.Release();

    if (allGood && structTpl->GetFieldCount() == 0)
    {
        GetLogger()->LogError("no fields defined for structure '%s'", name.c_str());
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

        if (attrName == "name")
        {
            name = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "offset")
        {
            OString offsetStr = static_cast<bstr_t>(attrNode->nodeTypedValue);

            // FIXME: there's gotta be a more sensible way to do this
            char *endPtr = NULL;
            offset = strtoul(offsetStr.c_str(), &endPtr, 0);
            if (endPtr == offsetStr.c_str() || offset < 0)
            {
                GetLogger()->LogError("invalid offset specified for structure field");
                return false;
            }
        }
        else if (attrName == "type")
        {
            typeName = static_cast<bstr_t>(attrNode->nodeTypedValue);

            if (!Marshaller::Factory::Instance()->HasMarshaller(typeName))
            {
                GetLogger()->LogError("unknown type '%s' specified for structure field", typeName.c_str());
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
        GetLogger()->LogError("name blank or not specified for structure field");
        return false;
    }
    else if (offset == -1)
    {
        GetLogger()->LogError("offset not specified for structure field");
        return false;
    }
    else if (typeName.size() == 0)
    {
        GetLogger()->LogError("type name not specified for structure field");
        return false;
    }

    return true;
}

void
HookManager::ParseEnumerationNode(MSXML2::IXMLDOMNodePtr &enumNode)
{
    OString name;
    MSXML2::IXMLDOMNodePtr attr = enumNode->attributes->getNamedItem("name");
    if (attr == NULL)
    {
        GetLogger()->LogError("name not specified for enumeration");
        return;
    }

    name = static_cast<bstr_t>(attr->nodeTypedValue);
    if (name.size() == 0)
    {
        GetLogger()->LogError("empty name specified for enumeration");
        return;
    }

    Marshaller::Enumeration *enumTpl = new Marshaller::Enumeration(name.c_str(), new Marshaller::UInt32(true), NULL);

    MSXML2::IXMLDOMNodeListPtr nodeList;

    nodeList = enumNode->childNodes;
    for (int i = 0; i < nodeList->length; i++)
    {
        MSXML2::IXMLDOMNodePtr node = nodeList->item[i];

        OString nodeName = node->nodeName;
        if (nodeName == "member")
        {
            OString memberName;
            DWORD memberValue;

            if (ParseEnumerationMemberNode(node, memberName, memberValue))
            {
                if (!enumTpl->AddMember(memberName, memberValue))
                {
                    GetLogger()->LogWarning("%s: failed to add member %s with value 0x%x, the value has already got a mapping",
                        name.c_str(), memberName.c_str(), memberValue);
                }
            }
        }
        else if (node->nodeType == MSXML2::NODE_ELEMENT)
        {
            GetLogger()->LogWarning("unknown enumeration subelement '%s'", nodeName.c_str());
        }
    }
    nodeList.Release();

    if (enumTpl->GetMemberCount() > 0)
    {
        TypeBuilder::Instance()->AddType(enumTpl);
    }
    else
    {
        GetLogger()->LogError("no valid members defined for enumeration '%s'", name.c_str());
        delete enumTpl;
    }
}

bool
HookManager::ParseEnumerationMemberNode(MSXML2::IXMLDOMNodePtr &enumMemberNode, OString &memberName, DWORD &memberValue)
{
    MSXML2::IXMLDOMNamedNodeMapPtr attrs = enumMemberNode->attributes;
    MSXML2::IXMLDOMNodePtr attr;

    attr = attrs->getNamedItem("name");
    if (attr == NULL)
    {
        GetLogger()->LogError("name not specified for enumeration member");
        return false;
    }
    memberName = static_cast<bstr_t>(attr->nodeTypedValue);

    attr = attrs->getNamedItem("value");
    if (attr == NULL)
    {
        GetLogger()->LogError("value not specified for enumeration member");
        return NULL;
    }
    OString valStr = static_cast<bstr_t>(attr->nodeTypedValue);

    // FIXME: there's gotta be a more sensible way to do this
    char *endPtr = NULL;
    memberValue = strtoul(valStr.c_str(), &endPtr, 0);
    if (endPtr == valStr.c_str())
    {
        GetLogger()->LogError("invalid value specified for enumeration member");
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

        if (attrName == "id")
        {
            id = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "name")
        {
            name = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "callingConvention")
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
                GetLogger()->LogWarning("unknown callingConvention '%s'", convName.c_str());
                continue;
            }
        }
        else if (attrName == "argsSize")
        {
            argsSize = attrNode->nodeTypedValue;
        }
        else
        {
            if (!ignoreUnknown)
            {
                GetLogger()->LogWarning("unknown functionSpec attribute '%s'", attrName.c_str());
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
            if (nodeName == "returnValue")
            {
                OString retTypeName;
                PropertyList retTypeProps;

                attrs = node->attributes;
                while ((attrNode = attrs->nextNode()) != NULL)
                {
                    OString attrName = static_cast<OString>(attrNode->nodeName);

                    if (attrName == "type")
                    {
                        retTypeName = static_cast<bstr_t>(attrNode->nodeTypedValue);
                    }
                    else
                    {
                        retTypeProps.push_back(pair<OString, OString>(attrName, OString(static_cast<bstr_t>(attrNode->nodeTypedValue))));
                    }
                }

                if (retTypeName.size() > 0 && Marshaller::Factory::Instance()->HasMarshaller(retTypeName))
                {
                    BaseMarshaller *marshaller = Marshaller::Factory::Instance()->CreateMarshaller(retTypeName);

                    for (PropertyList::const_iterator iter = retTypeProps.begin(); iter != retTypeProps.end(); iter++)
                    {
                        if (!marshaller->SetProperty(iter->first, iter->second))
                        {
                            GetLogger()->LogWarning("%s: failed to set property '%s' to '%s' on returnValue Marshaller::%s",
                                name.c_str(), iter->first.c_str(), iter->second.c_str(), retTypeName.c_str());
                        }
                    }

                    funcSpec->SetReturnValueMarshaller(marshaller);
                }
                else
                {
                    GetLogger()->LogWarning("%s: blank or invalid type name specified for returnValue", name.c_str());
                }
            }
            else if (nodeName == "arguments")
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
                    if (subNodeName == "argument")
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
                    else if (subNode->nodeType == MSXML2::NODE_ELEMENT)
                    {
                        GetLogger()->LogWarning("unknown arguments subelement '%s'", subNodeName.c_str());
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
            else if (node->nodeType == MSXML2::NODE_ELEMENT)
            {
                GetLogger()->LogWarning("unknown functionSpec subelement '%s'", nodeName.c_str());
            }
        }
        nodeList.Release();
        nodeList = NULL;
    }
    else
    {
        GetLogger()->LogError("name is blank or not specified for functionSpec");
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

        if (attrName == "name")
        {
            argName = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "direction")
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
                GetLogger()->LogWarning("unknown direction '%s'", dirStr.c_str());
            }
        }
        else if (attrName == "type")
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
        ss << "arg" << (argIndex + 1);
        argName = ss.str();
    }

    if (argDir == ARG_DIR_UNKNOWN)
    {
        GetLogger()->LogError("argument direction not specified");
        return NULL;
    }
    else if (argType.size() == 0)
    {
        GetLogger()->LogError("argument type is blank or not specified");
        return NULL;
    }

    BaseMarshaller *marshaller = Marshaller::Factory::Instance()->CreateMarshaller(argType);
    if (marshaller == NULL)
    {
        GetLogger()->LogError("argument type '%s' is unknown", argType.c_str());
        return NULL;
    }

    for (PropertyList::const_iterator iter = argTypeProps.begin(); iter != argTypeProps.end(); iter++)
    {
        if (!marshaller->SetProperty(iter->first, iter->second))
        {
            GetLogger()->LogWarning("failed to set property '%s' to '%s' on Marshaller::%s",
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

        if (attrName == "id")
        {
            id = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else if (attrName == "methodCount")
        {
            methodCount = attrNode->nodeTypedValue;
        }
        else
        {
            GetLogger()->LogWarning("unknown vtableSpec attribute '%s'", attrName.c_str());
            continue;
        }
    }

    attrs.Release();
    attrs = NULL;

    if (id.size() == 0)
    {
        GetLogger()->LogError("vtableSpec id is blank or not specified");
        return;
    }
    else if (methodCount <= 0)
    {
        GetLogger()->LogError("vtableSpec methodCount is invalid or not specified");
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
        if (nodeName == "method")
        {
            MSXML2::IXMLDOMNamedNodeMapPtr attrs = node->attributes;
            MSXML2::IXMLDOMNodePtr indexAttr = attrs->getNamedItem("index");

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
                GetLogger()->LogWarning("required vmethodSpec attribute 'index' is invalid or not specified");
            }

            attrs.Release();
        }
        else if (node->nodeType == MSXML2::NODE_ELEMENT)
        {
            GetLogger()->LogWarning("unknown vtableSpec subelement '%s'", nodeName.c_str());
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
    MSXML2::IXMLDOMNodePtr attr = sigNode->attributes->getNamedItem("name");
    if (attr == NULL)
    {
        GetLogger()->LogError("name not specified for signature");
        return;
    }

    name = static_cast<bstr_t>(attr->nodeTypedValue);
    if (name.size() == 0)
    {
        GetLogger()->LogError("empty name specified for signature");
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
        GetLogger()->LogError("signature definition '%s' is empty", name.c_str());
        return;
    }

    try
    {
        m_signatures[name] = new Signature(sigStr);
    }
    catch (Error &e)
    {
        GetLogger()->LogError("signature definition '%s' is invalid: %s", name.c_str(), e.what());
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

        if (attrName == "name")
        {
            name = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else
        {
            GetLogger()->LogWarning("unknown dllModule attribute '%s'", attrName.c_str());
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
        GetLogger()->LogError("name not specified for dllModule");
        return;
    }

    MSXML2::IXMLDOMNodePtr node;
    MSXML2::IXMLDOMNodeListPtr nodeList;
    
    nodeList = dllModNode->childNodes;
    for (int i = 0; i < nodeList->length; i++)
    {
        node = nodeList->item[i];

        OString nodeName = node->nodeName;
        if (nodeName == "function")
        {
            ParseDllFunctionNode(dllMod, node);
        }
        else if (node->nodeType == MSXML2::NODE_ELEMENT)
        {
            GetLogger()->LogWarning("unknown dllModule subelement '%s'", nodeName.c_str());
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

        if (attrName == "specId")
        {
            specId = static_cast<bstr_t>(attrNode->nodeTypedValue);
        }
        else
        {
            GetLogger()->LogWarning("unknown dllFunction attribute '%s'", attrName.c_str());
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
            GetLogger()->LogError("specId '%s' not found", specId.c_str());
        }
    }
    else
    {
        GetLogger()->LogError("specId not specified for dllFunction");
    }
}

void
HookManager::ParseFunctionNode(const OString &processName, MSXML2::IXMLDOMNodePtr &funcNode)
{
    OString specId, sigId;
    int sigOffset = 0;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = funcNode->attributes;
    MSXML2::IXMLDOMNodePtr attr;

    attr = attrs->getNamedItem("specId");
    if (attr == NULL)
    {
        GetLogger()->LogError("function spec id not specified for function");
        return;
    }
    specId = static_cast<bstr_t>(attr->nodeTypedValue);

    if (m_funcSpecs.find(specId) == m_funcSpecs.end())
    {
        GetLogger()->LogError("invalid function spec id '%s' specified for function", specId.c_str());
        return;
    }

    attr = attrs->getNamedItem("sigId");
    if (attr == NULL)
    {
        GetLogger()->LogError("signature id not specified for function");
        return;
    }
    sigId = static_cast<bstr_t>(attr->nodeTypedValue);

    SignatureMap::const_iterator iter = m_signatures.find(sigId);
    if (iter == m_signatures.end())
    {
        GetLogger()->LogError("invalid signature id '%s' specified for function", sigId.c_str());
        return;
    }

    const Signature *sig = iter->second;

    attr = attrs->getNamedItem("sigOffset");
    if (attr != NULL)
    {
        OString sigOffsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

        char *endPtr = NULL;
        sigOffset = strtol(sigOffsetStr.c_str(), &endPtr, 0);
        if (endPtr == sigOffsetStr.c_str())
        {
            GetLogger()->LogError("invalid signature offset specified for function");
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
        GetLogger()->LogError("signature '%s' specified for function not found: %s", sigId.c_str(), e.what());
        return;
    }

    DWORD offset = reinterpret_cast<DWORD>(startAddr) + sigOffset;

    GetLogger()->LogDebug("function with signature id '%s' found at offset 0x%x",
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

    attr = attrs->getNamedItem("specId");
    if (attr == NULL)
    {
        GetLogger()->LogError("specId not specified for vtable");
        return;
    }
    specId = static_cast<bstr_t>(attr->nodeTypedValue);

    if (m_vtableSpecs.find(specId) == m_vtableSpecs.end())
    {
        GetLogger()->LogError("invalid specId specified for vtable");
        return;
    }

    attr = attrs->getNamedItem("name");
    if (attr == NULL)
    {
        GetLogger()->LogError("name not specified for vtable");
        return;
    }
    name = static_cast<bstr_t>(attr->nodeTypedValue);

    DWORD offset;

    attr = attrs->getNamedItem("ctorSigId");
    if (attr != NULL)
    {
        OString sigId = static_cast<bstr_t>(attr->nodeTypedValue);
        int sigOffset = 0;

        if (sigId.size() == 0)
        {
            GetLogger()->LogError("ctorSigId cannot be blank");
            return;
        }

        SignatureMap::const_iterator iter = m_signatures.find(sigId);
        if (iter == m_signatures.end())
        {
            GetLogger()->LogError("constructor signature id '%s' not found", sigId.c_str());
            return;
        }

        const Signature *sig = iter->second;

        attr = attrs->getNamedItem("ctorSigOffset");
        if (attr != NULL)
        {
            OString sigOffsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

            char *endPtr = NULL;
            sigOffset = strtol(sigOffsetStr.c_str(), &endPtr, 0);
            if (endPtr == sigOffsetStr.c_str())
            {
                GetLogger()->LogError("invalid ctorSigOffset specified for vtable '%s'", name.c_str());
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
            GetLogger()->LogError("constructor signature specified for vtable '%s' not found: %s", name.c_str(), e.what());
            return;
        }

        startAddr = static_cast<char *>(startAddr) + sigOffset;
        offset = *(static_cast<DWORD *>(startAddr));

        if (!Util::Instance()->AddressIsWithinModule(offset, processName.c_str()))
        {
            GetLogger()->LogError("sigOffset specified for vtable '%s' seems to be invalid, pointer offset=0x%p, vtable offset=0x%08x",
                                  name.c_str(), startAddr, offset);
            return;
        }

        GetLogger()->LogDebug("vtable '%s': found at offset 0x%08x", name.c_str(), offset);
    }
    else
    {
        attr = attrs->getNamedItem("offset");
        if (attr == NULL)
        {
            GetLogger()->LogError("neither ctorSigId nor offset specified for vtable '%s'", name.c_str());
            return;
        }
        offsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

        // FIXME: there's gotta be a more sensible way to do this
        char *endPtr = NULL;
        offset = strtoul(offsetStr.c_str(), &endPtr, 0);
        if (endPtr == offsetStr.c_str())
        {
            GetLogger()->LogError("invalid offset specified for vtable");
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