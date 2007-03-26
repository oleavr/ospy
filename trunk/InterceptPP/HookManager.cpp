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

        MSXML2::IXMLDOMNodeListPtr nodeList;
        MSXML2::IXMLDOMNodePtr node;

        nodeList = doc->selectNodes("/HookManager/Types/*");
        for (int i = 0; i < nodeList->length; i++)
        {
            ParseTypeNode(nodeList->item[i]);
        }
        nodeList.Release();

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

        nodeList = doc->selectNodes("/HookManager/Specs/VTables/VTable");
        for (int i = 0; i < nodeList->length; i++)
        {
            ParseVTableSpecNode(nodeList->item[i]);
        }
        nodeList.Release();

        nodeList = doc->selectNodes("/HookManager/Hooks/DllModule");
        for (int i = 0; i < nodeList->length; i++)
        {
            ParseDllModuleNode(nodeList->item[i]);
        }
        nodeList.Release();

        OOStringStream ss;
        ss << "/HookManager/Hooks/VTables[@ProcessName = translate('";
        ss << Util::GetProcessName();
        ss << "', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')]/VTable";

        nodeList = doc->selectNodes(ss.str().c_str());
        for (int i = 0; i < nodeList->length; i++)
        {
            ParseVTableNode(nodeList->item[i]);
        }
        nodeList.Release();

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

    StructureDef *structDef = new StructureDef(name);

    MSXML2::IXMLDOMNodeListPtr nodeList;
    
    bool allGood = true;
    int fieldCount = 0;
    nodeList = structNode->childNodes;
    for (int i = 0; i < nodeList->length && allGood; i++)
    {
        MSXML2::IXMLDOMNodePtr node = nodeList->item[i];

        OString nodeName = node->nodeName;
        if (nodeName == "Field")
        {
            StructureFieldDef *fieldDef = ParseStructureFieldNode(node);
            if (fieldDef != NULL)
            {
                structDef->AddField(fieldDef);
                fieldCount++;
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
    nodeList = NULL;

    if (allGood && fieldCount == 0)
    {
        GetLogger()->LogError("No fields defined for structure '%s'", name.c_str());
        allGood = false;
    }

    if (!allGood)
    {
        delete structDef;
        return;
    }

    StructureBuilder::Instance()->AddStructure(structDef);
}

StructureFieldDef *
HookManager::ParseStructureFieldNode(MSXML2::IXMLDOMNodePtr &fieldNode)
{
    OString name, offsetStr, typeName;

    MSXML2::IXMLDOMNamedNodeMapPtr attrs = fieldNode->attributes;
    MSXML2::IXMLDOMNodePtr attr;

    attr = attrs->getNamedItem("Name");
    if (attr == NULL)
    {
        GetLogger()->LogError("Name not specified for Structure field");
        return NULL;
    }
    name = static_cast<bstr_t>(attr->nodeTypedValue);

    attr = attrs->getNamedItem("Offset");
    if (attr == NULL)
    {
        GetLogger()->LogError("Offset not specified for Structure field");
        return NULL;
    }
    offsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

    // FIXME: there's gotta be a more sensible way to do this
    char *endPtr = NULL;
    DWORD offset = strtol(offsetStr.c_str(), &endPtr, 0);
    if (endPtr == offsetStr.c_str())
    {
        GetLogger()->LogError("Invalid offset specified for Structure field");
        return NULL;
    }

    attr = attrs->getNamedItem("Type");
    if (attr == NULL)
    {
        GetLogger()->LogError("Type not specified for Structure field");
        return NULL;
    }
    typeName = static_cast<bstr_t>(attr->nodeTypedValue);

    if (!Marshaller::Factory::Instance()->HasMarshaller(typeName))
    {
        GetLogger()->LogError("Unknown type '%s' specified for Structure field", typeName.c_str());
        return NULL;
    }

    return new StructureFieldDef(name, offset, typeName);
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
        EnumerationBuilder::Instance()->AddEnumeration(enumTpl);
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
    memberValue = strtol(valStr.c_str(), &endPtr, 0);
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

            continue;
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
    OMap<OString, OString>::Type argTypeProps;

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
            argTypeProps[attrName] = static_cast<bstr_t>(attrNode->nodeTypedValue);
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

    OMap<OString, OString>::Type::iterator iter;
    for (iter = argTypeProps.begin(); iter != argTypeProps.end(); iter++)
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
    MSXML2::IXMLDOMNodePtr node;
    
    nodeList = vtSpecNode->childNodes;
    for (int i = 0; i < nodeList->length; i++)
    {
        node = nodeList->item[i];

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
HookManager::ParseVTableNode(MSXML2::IXMLDOMNodePtr &vtNode)
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

    attr = attrs->getNamedItem("Offset");
    if (attr == NULL)
    {
        GetLogger()->LogError("Offset not specified for VTable");
        return;
    }
    offsetStr = static_cast<bstr_t>(attr->nodeTypedValue);

    // FIXME: there's gotta be a more sensible way to do this
    char *endPtr = NULL;
    DWORD offset = strtol(offsetStr.c_str(), &endPtr, 0);
    if (endPtr == offsetStr.c_str())
    {
        GetLogger()->LogError("Invalid offset specified for VTable");
        return;
    }

    VTable *vtable = new VTable(m_vtableSpecs[specId], name, offset);
    m_vtables.push_back(vtable);
    vtable->Hook();
}

EnumerationBuilder *
EnumerationBuilder::Instance()
{
    static EnumerationBuilder *builder = NULL;

    if (builder == NULL)
        builder = new EnumerationBuilder();

    return builder;
}

EnumerationBuilder::~EnumerationBuilder()
{
    for (EnumTypeMap::iterator iter = m_enumTypes.begin(); iter != m_enumTypes.end(); iter++)
    {
        Marshaller::Factory::Instance()->UnregisterMarshaller(iter->first);
        delete iter->second;
    }
}

void
EnumerationBuilder::AddEnumeration(Marshaller::Enumeration *enumTemplate)
{
    m_enumTypes[enumTemplate->GetName()] = enumTemplate;
    Marshaller::Factory::Instance()->RegisterMarshaller(enumTemplate->GetName(), BuildEnumerationWrapper);
}

BaseMarshaller *
EnumerationBuilder::BuildEnumerationWrapper(const OString &name)
{
    return Instance()->BuildEnumeration(name);
}

BaseMarshaller *
EnumerationBuilder::BuildEnumeration(const OString &name)
{
    EnumTypeMap::iterator iter = m_enumTypes.find(name);
    if (iter == m_enumTypes.end())
        return NULL;

    return new Marshaller::Enumeration(*(iter->second));
}

StructureDef::~StructureDef()
{
    for (FieldDefList::iterator iter = m_fields.begin(); iter != m_fields.end(); iter++)
    {
        delete *iter;
    }
}

Marshaller::Structure *
StructureDef::CreateInstance() const
{
    Marshaller::Structure *structMarshaller = new Marshaller::Structure(m_name.c_str(), NULL);

    for (FieldDefList::const_iterator iter = m_fields.begin(); iter != m_fields.end(); iter++)
    {
        structMarshaller->AddField((*iter)->CreateInstance());
    }

    return structMarshaller;
}

StructureFieldDef::StructureFieldDef(const OString &name, DWORD offset, const OString &typeName)
    : m_name(name), m_offset(offset), m_typeName(typeName)
{
}

Marshaller::StructureField *
StructureFieldDef::CreateInstance()
{
    BaseMarshaller *marshaller = Marshaller::Factory::Instance()->CreateMarshaller(m_typeName);
    if (marshaller == NULL)
        return NULL;

    return new Marshaller::StructureField(m_name, m_offset, marshaller);
}

StructureBuilder *
StructureBuilder::Instance()
{
    static StructureBuilder *builder = NULL;

    if (builder == NULL)
        builder = new StructureBuilder();

    return builder;
}

StructureBuilder::~StructureBuilder()
{
    for (StructDefMap::iterator iter = m_structDefs.begin(); iter != m_structDefs.end(); iter++)
    {
        Marshaller::Factory::Instance()->UnregisterMarshaller(iter->second->GetName());
        delete iter->second;
    }
}

void
StructureBuilder::AddStructure(StructureDef *structDef)
{
    m_structDefs[structDef->GetName()] = structDef;
    Marshaller::Factory::Instance()->RegisterMarshaller(structDef->GetName(), BuildStructureWrapper);
    Marshaller::Factory::Instance()->RegisterMarshaller(structDef->GetName() + "Ptr", BuildStructurePtrWrapper);
}

BaseMarshaller *
StructureBuilder::BuildStructureWrapper(const OString &name)
{
    return Instance()->BuildStructure(name);
}

BaseMarshaller *
StructureBuilder::BuildStructure(const OString &name)
{
    StructDefMap::iterator iter = m_structDefs.find(name);
    if (iter == m_structDefs.end())
        return NULL;

    return iter->second->CreateInstance();
}

BaseMarshaller *
StructureBuilder::BuildStructurePtrWrapper(const OString &name)
{
    return Instance()->BuildStructurePtr(name);
}

BaseMarshaller *
StructureBuilder::BuildStructurePtr(const OString &name)
{
    OString typeName = name.substr(0, name.size() - 3);

    StructDefMap::iterator iter = m_structDefs.find(typeName);
    if (iter == m_structDefs.end())
        return NULL;

    return new Marshaller::Pointer(iter->second->CreateInstance());
}

} // namespace InterceptPP