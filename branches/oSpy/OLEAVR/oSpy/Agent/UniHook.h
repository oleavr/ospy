#pragma once

#include "Util.h"

class CHooker : public CObject {
public:
	CHooker();
	~CHooker() {}

	static void Init();
	static CHooker *Self();

	void HookAllModules();
	void HookModule(const OString &name);
	bool HookFunction(const OString &name, void *address);

	static void *m_ourStartAddress;
	static void *m_ourEndAddress;
private:
	// Glue code
	static void Stage2Proxy();
	static void PreExecRedirProxy(void *callerAddress);
	static void Stage3Proxy();

	// Handlers
	void PreExecProxy(void *callerAddress, void *retAddr, void *args, DWORD lastError);
	void PostExecProxy(void *callerAddress, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError);

	OMap<LPVOID, OString>::Type m_hookedAddrToName;
};
