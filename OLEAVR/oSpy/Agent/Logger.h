#pragma once

#include "Util.h"

class CLogger : public CObject
{
public:
	CLogger();
	~CLogger();

	static CLogger *Self();

	void LogFunctionCall(const OString &functionName, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError);
private:
	SLIST_HEADER m_msgQueue;
};
