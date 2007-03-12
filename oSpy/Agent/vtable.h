#pragma once

#include "hooking.h"

typedef struct {
	DWORD index;
	DWORD functionStart;
} VFuncContext;

class VTableHooker : BaseObject
{
public:
	static VTableHooker *Self();

	void HookVTableAt(void *startOffset, int numFuncs);

protected:
	static void VTableProxyFunc(CpuContext cpuCtx, VFuncContext *funcCtx);
};
