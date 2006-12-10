#pragma once

#include "Util.h"
#include "IPCClient.h"

class CLogger : public CObject
{
public:
	CLogger();
	~CLogger();

	static void Init();
	static CLogger *Self();

	void LogFunctionCall(const OString &functionName, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError);
private:
	IPCClient *m_client;
};
