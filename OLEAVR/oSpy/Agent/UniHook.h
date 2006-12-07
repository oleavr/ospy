#pragma once

#include "Util.h"

class CHooker {
public:
	CHooker();
	~CHooker() {}

	static CHooker *Self();

	void UpdateModuleList();
	void HookAllModules();
	void HookModule(const OString &name);
	bool HookFunction(const OString &name, void *address);

private:
	// Glue code
	static void Stage2Proxy();
	static void PreExecRedirProxy(void *callerAddress);
	static void Stage3Proxy();

	// Handlers
	void PreExecProxy(void *callerAddress, void *retAddr, void *args, DWORD lastError);
	void PostExecProxy(void *callerAddress, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError);

	OVector<OICString>::Type m_modules;
	OMap<LPVOID, OString>::Type m_hookedAddrToName;
};
