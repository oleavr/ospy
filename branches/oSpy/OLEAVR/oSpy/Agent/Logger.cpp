#include "StdAfx.h"
#include "Logger.h"

CLogger::CLogger()
{
	InitializeSListHead(&m_msgQueue);
}

CLogger::~CLogger()
{
}

CLogger *
CLogger::Self()
{
	static CLogger *logger = new CLogger();
	return logger;
}

void
CLogger::LogFunctionCall(const OString &functionName, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError)
{
}
