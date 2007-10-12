//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

#pragma once

#include "Core.h"
#include "Marshallers.h"
#include "VTable.h"
#include "DLL.h"
#import <msxml6.dll>

namespace InterceptPP {

class StructureFieldDef;

class HookManager : public BaseObject {
public:
	HookManager();
    ~HookManager();

    static HookManager *Instance();

    void LoadDefinitions(const OWString &path);

    void Reset();

    unsigned int GetFunctionSpecCount() const { return static_cast<unsigned int>(m_funcSpecs.size()); }
    unsigned int GetVTableSpecCount() const { return static_cast<unsigned int>(m_vtableSpecs.size()); }
    unsigned int GetSignatureCount() const { return static_cast<unsigned int>(m_signatures.size()); }
    unsigned int GetDllModuleCount() const { return static_cast<unsigned int>(m_dllModules.size()); }
    unsigned int GetDllFunctionCount() const { return static_cast<unsigned int>(m_dllFunctions.size()); }
    unsigned int GetFunctionCount() const { return static_cast<unsigned int>(m_functions.size()); }
    unsigned int GetVTableCount() const { return static_cast<unsigned int>(m_vtables.size()); }

	FunctionSpec *GetFunctionSpecById(const OString &id);

protected:
    typedef OMap<OString, FunctionSpec *>::Type FunctionSpecMap;
    typedef OMap<OString, VTableSpec *>::Type VTableSpecMap;
    typedef OMap<OString, Signature *>::Type SignatureMap;
    typedef OMap<OICString, DllModule *>::Type DllModuleMap;
    typedef OList<DllFunction *>::Type DllFunctionList;
    typedef OList<Function *>::Type FunctionList;
    typedef OList<VTable *>::Type VTableList;
    typedef OList<pair<OString, OString>>::Type PropertyList;

    FunctionSpecMap m_funcSpecs;
    VTableSpecMap m_vtableSpecs;
    SignatureMap m_signatures;
    DllModuleMap m_dllModules;
    DllFunctionList m_dllFunctions;
    FunctionList m_functions;
    VTableList m_vtables;

    void ParseTypeNode(MSXML2::IXMLDOMNodePtr &typeNode);
    void ParseStructureNode(MSXML2::IXMLDOMNodePtr &structNode);
    bool ParseStructureFieldNode(MSXML2::IXMLDOMNodePtr &fieldNode, OString &name, int &offset, OString &typeName, PropertyList &typeProps);
    void ParseEnumerationNode(MSXML2::IXMLDOMNodePtr &enumNode);
    bool ParseEnumerationMemberNode(MSXML2::IXMLDOMNodePtr &enumMemberNode, OString &memberName, DWORD &memberValue);

    FunctionSpec *ParseFunctionSpecNode(MSXML2::IXMLDOMNodePtr &funcSpecNode, OString &id, bool nameRequired=true, bool ignoreUnknown=false);
    ArgumentSpec *ParseFunctionSpecArgumentNode(FunctionSpec *funcSpec, MSXML2::IXMLDOMNodePtr &argNode, int argIndex);
    void ParseVTableSpecNode(MSXML2::IXMLDOMNodePtr &vtSpecNode);
    void ParseSignatureNode(MSXML2::IXMLDOMNodePtr &sigNode);

    void ParseDllModuleNode(MSXML2::IXMLDOMNodePtr &dllModNode);
    void ParseDllFunctionNode(DllModule *dllMod, MSXML2::IXMLDOMNodePtr &dllFuncNode);
    void ParseFunctionNode(const OString &processName, MSXML2::IXMLDOMNodePtr &funcNode);
    void ParseVTableNode(const OString &processName, MSXML2::IXMLDOMNodePtr &vtNode);
};

class TypeBuilder
{
public:
    static TypeBuilder *Instance();
    ~TypeBuilder();

    void AddType(BaseMarshaller *tpl);

    unsigned int GetTypeCount() const { return static_cast<unsigned int>(m_types.size()); }

protected:
    typedef OMap<OString, BaseMarshaller *>::Type TypeMap;
    TypeMap m_types;

    static BaseMarshaller *BuildTypeWrapper(const OString &name);
    BaseMarshaller *BuildType(const OString &name);
};

} // namespace InterceptPP
