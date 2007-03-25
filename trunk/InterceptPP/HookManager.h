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

#pragma once

#include "Core.h"
#include "DLL.h"
#import <msxml6.dll>

namespace InterceptPP {

class HookManager : public BaseObject {
public:
	HookManager();
    ~HookManager();

    static HookManager *Instance();

    void LoadDefinitions(const OString &path);

    unsigned int GetFunctionSpecCount() const { return static_cast<unsigned int>(m_funcSpecs.size()); }

protected:
    typedef OMap<OString, FunctionSpec *>::Type FunctionSpecMap;
    typedef OMap<OICString, DllModule *>::Type DllModuleMap;
    typedef OList<DllFunction *>::Type DllFunctionList;

    FunctionSpecMap m_funcSpecs;
    DllModuleMap m_dllModules;
    DllFunctionList m_dllFunctions;

    void ParseFunctionSpecNode(MSXML2::IXMLDOMNodePtr &funcSpecNode);
    void ParseFunctionSpecArgumentNode(FunctionSpec *funcSpec, MSXML2::IXMLDOMNodePtr &argNode, int argIndex);
    void ParseDllModuleNode(MSXML2::IXMLDOMNodePtr &dllModNode);
    void ParseDllFunctionNode(DllModule *dllMod, MSXML2::IXMLDOMNodePtr &dllFuncNode);
};

} // namespace InterceptPP