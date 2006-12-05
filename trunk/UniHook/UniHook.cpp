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

	static void Stage2Proxy();

	static void PreExecRedirProxy(LPVOID caller_address);
	void PreExecProxy(LPVOID caller_address);

	static void Stage3Proxy();

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

// TAKE 1
#if 0
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

// TAKE 2
#if 0
	//
	// STEP 1: Save all registers
	//


	//
	// STEP 2: We need to make a copy of the stack frame.
	//
	// At this point the stack looks like this:
	//
	//    [  .....  ]  |
	//    [  arg_n  ]  |
	//    [  .....  ]  |
	//    [  arg_0  ]  |
	//    [ retaddr ] \|/
	//    [  sregs  ]  V
	//
    // And we make it look like this:
    //
	//    [  .....  ]  |
	//    [  arg_n  ]  |
	//    [  .....  ]  |
	//    [  arg_0  ]  |
	//    [ retaddr ]  |
	//    [  sregs  ]  |
	//    [  .....  ]  |
	//    [  arg_n  ]  |
	//    [  .....  ]  |
	//    [  arg_0  ] \|/
	//    [ retaddr ]  V
    //
	// Why are we doing this?
	//   1) Because the API function might use any of the arguments' stack
	//      space for temporary storage when it's done with them, we make
	//      a copy of the whole thing so that the function won't clobber
	//      the original arguments.
	//   2) We want to conveniently trap the return.
	//
	//      This is also the perfect time to figure out how many bytes of
	//      arguments the function actually consumed as API functions in
	//      general are stdcall (diff == 0 => cdecl).  We figure this out
	//      by taking ESP_before, which we save in ESI prior to calling
	//      the function, and compare it to ESP_now. The size of the
	//      arguments will be ESP_now - ESP_before - sizeof(LPVOID),
	//      where the latter is the return address.
	//
	// NOTE:
	// The number of bytes to copy could be determined heuristically
	// by walking up the stack looking for valid pointers and finding
	// one pointing to an instruction following a CALL (to find the
	// previous stack frame), but this is kind of risky so we'll instead
	// copy a "safe" number of bytes and hope we got it all (and leave
	// the number of bytes tuneable).
	//

	memcpy(proxy + offset, templ_copy_stack_frame, sizeof(templ_copy_stack_frame));
	*((DWORD *) (proxy + offset + 1)) = MAX_FRAME_SIZE / 4;
	*((DWORD *) (proxy + offset + 7)) = MAX_FRAME_SIZE;
	*((DWORD *) (proxy + offset + 20)) = MAX_FRAME_SIZE;
	offset += sizeof(templ_copy_stack_frame);


	//
	// STEP 3: Call the pre-execution proxy
	//

	// push <absolute address of the hooked function>
	proxy[offset++] = 0x68;
	*((DWORD *) (proxy + offset)) = (DWORD) address;
	offset += 4;

	// call
	proxy[offset++] = 0xE8;
	*((DWORD *) (proxy + offset)) = (DWORD) &ProxyFunction - (DWORD) (proxy + offset + 4);
	offset += 4;

	// clean up after the call (cdecl)
	proxy[offset++] = 0x81;			   // ADD
	proxy[offset++] = 0xC4;			   // ESP
	*((DWORD *) (proxy + offset)) = 4; // 4
	offset += 4;


	//
	// STEP 4: Overwrite the return address on the copy so that the API
    //         function returns to our stage 2 proxy instead of the
	//         caller.
	//

	// mov [esp], offsetof STEPX
	proxy[offset++] = 0xc7;
	proxy[offset++] = 0x04;
	proxy[offset++] = 0x24;
	*((DWORD *) (proxy + offset)) = 0; // FIXME
	offset += 4;


	//
	// STEP 5: Save ESP in ESI.
	//

	// mov esi, esp
	proxy[offset++] = 0x89;
	proxy[offset++] = 0xe6;


	//
	// STEP 6: Do what the original function did where we overwrote it and JMP
	//         to the next instruction thereafter.
	//

	// The instructions that we overwrote
	memcpy(proxy + offset, sig, sizeof(sig));
	offset += sizeof(sig);

	// And finally JMP to the next instruction in the original
	proxy[offset++] = 0xE9;
	*((DWORD *) (proxy + offset)) = ((DWORD) address + sizeof(sig)) - (DWORD) (proxy + offset + 4);
	offset += 4;

	//
	// STEP 7: The API function has returned to us.
	//


	//
	// PATCH THE START OF THE ORIGINAL FUNCTION TO BOUNCE TO THE START OF THE
	// PROXY *DRUMROLL*
	//
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

static const unsigned char templ_copy_stack_frame[] = {
	0xb9, 0xff, 0xff, 0xff, 0xff,       // <00> mov ecx, (MAX_FRAME_SIZE / 4)
	0x81, 0xec, 0xff, 0xff, 0xff, 0xff, // <05> sub esp, MAX_FRAME_SIZE
	0x56,                               // <11> push esi
	0x57,                               // <12> push edi
	0xfc,                               // <13> cld
	0x8d, 0x7c, 0x24, 0x08,             // <14> lea edi, [esp + 8]
	0x8d, 0xb7, 0xff, 0xff, 0xff, 0xff, // <18> lea esi, [edi + MAX_FRAME_SIZE]
	0xf3, 0xa5,                         // <24> rep movsd
	0x5f,                               // <26> pop edi
	0x5e                                // <27> pop esi
};
#endif

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


	//
	// Generate the per-function stage 1 proxy which will have the following
    // layout:
	//
	// [code1]   <-- fixed size
	// [data]    <-- fixed size
	// [code2]   <-- variable size (depending on function)
	//
	// Where code1 is where it all starts.  The intercepted function will JMP
	// to it and it will:
	//   1) Set ecx to point to <data>.
	//   2) JMP to the stage2 proxy.
	//

	unsigned char *proxy = new unsigned char[1 + sizeof(DWORD) + 1 + sizeof(DWORD) +
											 sizeof(DWORD) +
											 sizeof(sig) + 1 + sizeof(DWORD)];
	int offset = 0;

	//
	// code1
	//

	// mov ecx, <proxy.data's address>
	proxy[offset++] = 0xbe;
	*((DWORD *) (proxy + offset)) = (DWORD) (proxy + 10);
	offset += sizeof(DWORD);

	// jmp stage 2
	proxy[offset++] = 0xe9;
	*((DWORD *) (proxy + offset)) = ((DWORD) CHooker::Stage2Proxy) - (DWORD) (proxy + offset + 4);
	offset += sizeof(DWORD);

	//
	// data
	//

	// just the absolute address of the intercepted function
	*((DWORD *) (proxy + offset)) = (DWORD) address;
	offset += sizeof(DWORD);

	//
	// code2
	//

	// the overwritten prolog code of the intercepted function
	memcpy(proxy + offset, sig, sizeof(sig));
	offset += sizeof(sig);

	// and finally a JMP to the next instruction in the original
	proxy[offset++] = 0xe9;
	*((DWORD *) (proxy + offset)) = ((DWORD) address + sizeof(sig)) - (DWORD) (proxy + offset + 4);
	offset += sizeof(DWORD);


	//
	// Patch the start of the original function to bounce to the code1 section
	// of the dynamic proxy *DRUMROLL*
	//

	unsigned char buf[5];
	buf[0] = 0xe9;
	*((DWORD *) (buf + 1)) = (DWORD) proxy - (DWORD) address - sizeof(buf);

	HANDLE process;
	DWORD oldProtect, nWritten;

	process = GetCurrentProcess();
	VirtualProtect(address, sizeof(buf), PAGE_EXECUTE_WRITECOPY, &oldProtect);
	WriteProcessMemory(process, address, buf, sizeof(buf), &nWritten);
	FlushInstructionCache(process, NULL, 0);

	return true;
}

#define MAX_FRAME_SIZE  4096
#define SAVED_REGS_SIZE   32

__declspec (naked) void
CHooker::Stage2Proxy()
{
	__asm {
		//
		// STEP 1: Save all registers and store the dynamic proxy's address
		//         somewhere safe.
		//

		pushad;

		mov esi, ecx;


		//
		// STEP 2: We need to make a copy of the stack frame.
		//
		// At this point the stack looks like this:
		//
		//    [  .....  ]  |
		//    [  arg_n  ]  |
		//    [  .....  ]  |
		//    [  arg_0  ]  |
		//    [ retaddr ] \|/
		//    [  sregs  ]  V
		//
		// And we want to make it look like this:
		//
		//    [  .....  ]  |
		//    [  arg_n  ]  |
		//    [  .....  ]  |
		//    [  arg_0  ]  |
		//    [ retaddr ]  |
		//    [  sregs  ]  |
		//    [  .....  ]  |
		//    [  arg_n  ]  |
		//    [  .....  ]  |
		//    [  arg_0  ] \|/
		//    [ retaddr ]  V
		//
		// Why are we doing this?
		//   1) Because the API function might use any of the arguments' stack
		//      space for temporary storage when it's done with them, we make
		//      a copy of the whole thing so that the function won't clobber
		//      the original arguments.
		//   2) We want to conveniently trap the return.
		//
		//      This is also the perfect time to figure out how many bytes of
		//      arguments the function actually consumed as API functions in
		//      general are stdcall (diff == 0 => cdecl).  We figure this out
		//      by taking ESP_before, which we save in ESI prior to calling
		//      the function, and compare it to ESP_now. The size of the
		//      arguments will be ESP_now - ESP_before - sizeof(LPVOID),
		//      where the latter is the return address.
		//
		// NOTE:
		// The number of bytes to copy could be determined heuristically
		// by walking up the stack looking for valid pointers and finding
		// one pointing to an instruction following a CALL (to find the
		// previous stack frame), but this is kind of risky so we'll instead
		// copy a "safe" number of bytes and hope we got it all (and leave
		// the number of bytes tuneable).
		//

		mov ecx, (MAX_FRAME_SIZE / 4);
		sub esp, MAX_FRAME_SIZE;
		push esi;
		cld;
		lea edi, [esp + 4];
		lea esi, [edi + MAX_FRAME_SIZE + SAVED_REGS_SIZE];
		rep movsd;
		pop esi;


		//
		// STEP 3: Call the pre-execution proxy
		//

		push [esi]; // proxy.data -- the absolute address of the intercepted function
		call CHooker::PreExecRedirProxy;
		add esp, 4; // because it's cdecl we have to clean up the stack


		//
		// STEP 4: Overwrite the return address on the copy so that the API
		//         function returns to our stage 3 proxy instead of the
		//         caller.
		//
		
		mov [esp], offset CHooker::Stage3Proxy;


		//
		// STEP 5: Save ESP + 4 in EDI.
		//

		lea edi, [esp + 4];


		//
		// STEP 6: JMP to proxy.code2 to do what the original function did
		//         where we overwrote it and then bounce to the next
		//         instruction thereafter.
		//

		lea ecx, [esi + 4];
		jmp ecx;
   }
}

__declspec (naked) void
CHooker::Stage3Proxy()
{
	__asm {
		// ok...
	}
}

void
CHooker::PreExecRedirProxy(LPVOID caller_address)
{
	CHooker::self()->PreExecProxy(caller_address);
}

void
CHooker::PreExecProxy(LPVOID caller_address)
{
	cout << "PreExecProxy: called from " << this->hookedAddrToName[caller_address] << " (" <<  caller_address << ")" << endl;
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
