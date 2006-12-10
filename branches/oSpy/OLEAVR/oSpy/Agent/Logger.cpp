#include "StdAfx.h"
#include "Logger.h"

static CLogger *g_logger = NULL;

CLogger::CLogger()
{
	m_client = new IPCClient("messages");
}

CLogger::~CLogger()
{
}

void
CLogger::Init()
{
	g_logger = new CLogger();
}

CLogger *
CLogger::Self()
{
	return g_logger;
}

void
CLogger::LogFunctionCall(const OString &functionName, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError)
{
	ORPCBuffer buf;

	buf.AppendString(CUtil::GetProcessName());
	buf.AppendDWORD(GetCurrentProcessId());
	buf.AppendDWORD(GetCurrentThreadId());

	buf.AppendString(functionName);
	buf.AppendData(args, (unsigned short) argsSize);
	buf.AppendDWORD(retval);
	buf.AppendDWORD(lastError);

	buf.AppendDWORD((DWORD) retAddr);
	buf.AppendString(CUtil::GetModuleNameForAddress(retAddr));

	m_client->write((void *) buf.c_str(), (DWORD) buf.size());
}
