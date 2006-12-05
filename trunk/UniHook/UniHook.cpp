/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

#include "stdafx.h"
#include "Util.h"
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
	bool HookFunction(const string &name, LPVOID address);

private:
	static void Stage2Proxy();

	static void PreExecRedirProxy(LPVOID callerAddress) { CHooker::self()->PreExecProxy(callerAddress); }
	void PreExecProxy(LPVOID callerAddress);

	static void Stage3Proxy();

	void PostExecProxy(LPVOID callerAddress, DWORD retAddr, LPVOID args, DWORD argsSize, DWORD &retval);



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
	proxy[offset++] = 0xb9;
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
#define SAVED_REGS_SIZE   16

__declspec (naked) void
CHooker::Stage2Proxy()
{
	__asm {
		//
		// STEP 1: Save all registers that have to be left unclobbered, and
		//         store the dynamic proxy's address somewhere safe.
		//

		push ebx;
		push ebp;
		push esi;
		push edi;

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
		
		mov dword ptr [esp], offset CHooker::Stage3Proxy;


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

void
CHooker::PreExecProxy(LPVOID callerAddress)
{
	cout << "PreExecProxy: called from " << this->hookedAddrToName[callerAddress] << " (" <<  callerAddress << ")" << endl;
}

__declspec (naked) void
CHooker::Stage3Proxy()
{
	LPVOID callerAddress, args;
	DWORD retAddr, argsSize, retval;

	__asm {
		// Calculate the number of bytes of arguments consumed
		mov ecx, esp;
		sub ecx, edi;

		// And then clear off the stack so that it's back to the point where it was
		// after stage 2 step 1 (registers saved).
		mov ebx, MAX_FRAME_SIZE;
		sub ebx, 4;
		sub ebx, ecx;
		add esp, ebx;

		// Do the prolog dance
		push ebp;
		mov ebp, esp;
		sub esp, __LOCAL_SIZE;

		mov [argsSize], ecx;
		mov [retval], eax;

		mov edx, [esi];
		mov [callerAddress], edx;

		mov esi, ecx

		lea ecx, [ebp + 4 + SAVED_REGS_SIZE];
		mov edx, [ecx];
		mov [retAddr], edx;

		add ecx, 4;
		mov [args], ecx;
	}

	CHooker::self()->PostExecProxy(callerAddress, retAddr, args, argsSize, retval);

	__asm {
		// Just in case we want to change the return value
		mov eax, [retval];

		// And the epilog dance
		mov esp, ebp;
		pop ebp;

		mov ecx, esi;

		// Restore registers
		pop edi;
		pop esi;
		pop ebp;
		pop ebx;

		// Now we have to clear the stack like the original function would have
		mov edx, dword ptr [esp] // return address
		add esp, 4;    // clear off the return address
		add esp, ecx;  // ...and the arguments
		jmp edx;       // jump back to the caller
	}
}

void
CHooker::PostExecProxy(LPVOID callerAddress, DWORD retAddr, LPVOID args, DWORD argsSize, DWORD &retval)
{
	cout << "PostExecProxy: " << this->hookedAddrToName[callerAddress] << " (" <<  callerAddress << ") called with retAddr=" << retAddr << ", argsSize=" << argsSize << " and retval=" << (int) retval << endl;

	if (argsSize > 0)
		cout << "PostExecProxy:" << endl << hexdump(args, argsSize, 16);
}

typedef SOCKET (__stdcall *SocketFunc) (int af, int type, int protocol);

int _tmain(int argc, _TCHAR* argv[])
{
	HMODULE h = LoadLibrary(L"C:\\WINDOWS\\SYSTEM32\\ws2_32.dll");

	CHooker::self()->HookModule(L"C:\\WINDOWS\\SYSTEM32\\ws2_32.dll");

	WORD versionRequested = MAKEWORD(2, 2);
	WSADATA wsaData;

	WSAStartup(versionRequested, &wsaData);

	SocketFunc socketFunc = (SocketFunc) GetProcAddress(h, "socket");
	int s = socketFunc(AF_INET, SOCK_STREAM, 0);

	cout << "socket() returned " << s << endl;

	WSACleanup();

	return 0;
}
