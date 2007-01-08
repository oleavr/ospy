#pragma once

typedef struct {
	OICString name;
	void *startAddress;
	void *endAddress;
} OModuleInfo;

class CUtil
{
public:
	static void Init();

	static const OString &GetProcessName() { return m_processName; }
	static OString GetModuleNameForAddress(LPVOID address);
	static OModuleInfo GetModuleInfo(const OICString &name) { return m_modules[name]; }
	static OVector<OModuleInfo>::Type GetAllModules();

private:
	static void UpdateModuleList();

	static OString m_processName;
	static OMap<OICString, OModuleInfo>::Type m_modules;
};
