// UniHook.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Imagehlp.h>
#include <winsock2.h>
#include <iostream>
#include <map>
#include <string>

#pragma warning(disable: 4244 4311)

using std::cout;
using std::endl;
using std::map;
using std::string;

class CHooker {
public:
	static CHooker *self();
	void HookModule(TCHAR *name);
private:
	bool HookFunction(const string &name, LPVOID address);

	static void ProxyFunction(LPVOID caller_address);
	void RealProxyFunction(LPVOID caller_address);

	map<LPVOID, string> hookedAddrToName;
};

CHooker *
CHooker::self()
{
	static CHooker *hooker = new CHooker();
	return hooker;
}

void
CHooker::HookModule(TCHAR *name)
{
	HMODULE h = LoadLibrary(name);
	if (h == NULL)
		return;

	IMAGE_DOS_HEADER *dosHeader = (IMAGE_DOS_HEADER *) h;
	IMAGE_NT_HEADERS *peHeader = (IMAGE_NT_HEADERS *) ((char *) h + dosHeader->e_lfanew);
	IMAGE_EXPORT_DIRECTORY *expDir = (IMAGE_EXPORT_DIRECTORY *) ((char *) h + peHeader->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress);

	DWORD *names = (DWORD *) ((char *) h + expDir->AddressOfNames);
	WORD *ordinals = (WORD *) ((char *) h + expDir->AddressOfNameOrdinals);
	DWORD *functions = (DWORD *) ((char *) h + expDir->AddressOfFunctions);

	for (unsigned int i = 0; i < expDir->NumberOfNames; i++)
	{
		char *name = (char *) h + names[i];
		LPVOID addr = (unsigned char *) h + functions[ordinals[i]];

		HookFunction(name, addr);
	}

	cout << hookedAddrToName.size() << " out of " << expDir->NumberOfNames << " functions hooked" << endl;
}

#if false
bool
CHooker::HookFunction(const string &name, LPVOID address)
{
	unsigned char sig[] = {
		0x8B, 0xFF, // mov edi, edi
		0x55,       // push ebp
		0x8B, 0xEC, // mov ebp, esp
	};

	if (memcmp(address, sig, sizeof(sig)) != 0)
	{
		cout << "failed to hook function " << name << " at " << address << endl;
		return false;
	}

	hookedAddrToName[address] = name;

	unsigned char *proxy = new unsigned char[1 + 5 + 5 + 6 + 1 + sizeof(sig) + 5];

	int offset = 0;

	// PUSHAD
	proxy[offset++] = 0x60;

	// PUSH <absolute address of the hooked function>
	proxy[offset++] = 0x68;
	*((DWORD *) (proxy + offset)) = (DWORD) address;
	offset += 4;

	// CALL near, relative to the common proxy
	proxy[offset++] = 0xE8;
	*((DWORD *) (proxy + offset)) = (DWORD) &ProxyFunction - (DWORD) (proxy + offset + 4);
	offset += 4;

	// Clean up after the call (cdecl)
	proxy[offset++] = 0x81;			   // ADD
	proxy[offset++] = 0xC4;			   // ESP
	*((DWORD *) (proxy + offset)) = 4; // 4
	offset += 4;

	// POPAD
	proxy[offset++] = 0x61;

	// Do what the original function did where we overwrote it
	memcpy(proxy + offset, sig, sizeof(sig));
	offset += sizeof(sig);

	// JMP to after that part
	proxy[offset++] = 0xE9;
	*((DWORD *) (proxy + offset)) = ((DWORD) address + sizeof(sig)) - (DWORD) (proxy + offset + 4);
	offset += 4;

	// Time to patch
	unsigned char buf[5];
	buf[0] = 0xE9;
	*((DWORD *) (buf + 1)) = (DWORD) proxy - (DWORD) address - sizeof(buf);

	HANDLE process;
	DWORD oldProtect, nWritten;

	process = GetCurrentProcess();
	VirtualProtect(address, sizeof(buf), PAGE_EXECUTE_WRITECOPY, &oldProtect);
	WriteProcessMemory(process, address, buf, sizeof(buf), &nWritten);
	FlushInstructionCache(process, NULL, 0);

	return true;
}
#endif

#define MAX_STACK_SIZE 16384

bool
CHooker::HookFunction(const string &name, LPVOID address)
{
	unsigned char sig[] = {
		0x8B, 0xFF, // mov edi, edi
		0x55,       // push ebp
		0x8B, 0xEC, // mov ebp, esp
	};

	if (memcmp(address, sig, sizeof(sig)) != 0)
	{
		cout << "failed to hook function " << name << " at " << address << endl;
		return false;
	}

	hookedAddrToName[address] = name;

	unsigned char *proxy = new unsigned char[1 + 5 + 5 + 6 + 1 + sizeof(sig) + 5];

	int offset = 0;

	// PUSHAD
	proxy[offset++] = 0x60;

	// PUSH <absolute address of the hooked function>
	proxy[offset++] = 0x68;
	*((DWORD *) (proxy + offset)) = (DWORD) address;
	offset += 4;

	// CALL near, relative to the common proxy
	proxy[offset++] = 0xE8;
	*((DWORD *) (proxy + offset)) = (DWORD) &ProxyFunction - (DWORD) (proxy + offset + 4);
	offset += 4;

	// Clean up after the call (cdecl)
	proxy[offset++] = 0x81;			   // ADD
	proxy[offset++] = 0xC4;			   // ESP
	*((DWORD *) (proxy + offset)) = 4; // 4
	offset += 4;

	// POPAD
	proxy[offset++] = 0x61;

	// Do what the original function did where we overwrote it
	memcpy(proxy + offset, sig, sizeof(sig));
	offset += sizeof(sig);

	// JMP to after that part
	proxy[offset++] = 0xE9;
	*((DWORD *) (proxy + offset)) = ((DWORD) address + sizeof(sig)) - (DWORD) (proxy + offset + 4);
	offset += 4;

	// Time to patch
	unsigned char buf[5];
	buf[0] = 0xE9;
	*((DWORD *) (buf + 1)) = (DWORD) proxy - (DWORD) address - sizeof(buf);

	HANDLE process;
	DWORD oldProtect, nWritten;

	process = GetCurrentProcess();
	VirtualProtect(address, sizeof(buf), PAGE_EXECUTE_WRITECOPY, &oldProtect);
	WriteProcessMemory(process, address, buf, sizeof(buf), &nWritten);
	FlushInstructionCache(process, NULL, 0);

	return true;
}

void
CHooker::ProxyFunction(LPVOID caller_address)
{
	CHooker::self()->RealProxyFunction(caller_address);
}

void
CHooker::RealProxyFunction(LPVOID caller_address)
{
	cout << "ProxyFunction: called from " << this->hookedAddrToName[caller_address] << " (" <<  caller_address << ")" << endl;
}

typedef SOCKET (__stdcall *SocketFunc) (int af, int type, int protocol);

int _tmain(int argc, _TCHAR* argv[])
{
	HMODULE h = LoadLibrary(L"C:\\WINDOWS\\SYSTEM32\\ws2_32.dll");

	CHooker::self()->HookModule(L"C:\\WINDOWS\\SYSTEM32\\ws2_32.dll");

	SocketFunc socketFunc = (SocketFunc) GetProcAddress(h, "socket");
	int s = socketFunc(AF_INET, SOCK_STREAM, 0);

	return 0;
}
