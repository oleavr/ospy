//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

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
#include "SigMatch.h"
#include "Util.h"

#include <Imagehlp.h>
#include <winsock2.h>
#include <iostream>
#include <map>
#include <string>

#pragma warning(disable: 4244 4311)

using std::cout;
using std::cerr;
using std::endl;
using std::map;
using std::string;

class CHooker {
public:
	static CHooker *self();

	void HookModule(TCHAR *name);
	bool HookFunction(const string &name, void *address);

private:
	// Glue code
	static void Stage2Proxy();
	static void PreExecRedirProxy(void *callerAddress);
	static void Stage3Proxy();

	// Handlers
	void PreExecProxy(void *callerAddress, void *retAddr, void *args, DWORD lastError);
	void PostExecProxy(void *callerAddress, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError);

	HashTable<LPVOID, string>::Type m_hookedAddrToName;
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

	size_t hookCountBefore = m_hookedAddrToName.size();

	for (unsigned int i = 0; i < expDir->NumberOfNames; i++)
	{
		char *name = (char *) h + names[i];
		void *addr = (unsigned char *) h + functions[ordinals[i]];

		if (m_hookedAddrToName.find(addr) == m_hookedAddrToName.end())
			HookFunction(name, addr);
	}

	cout << "CHooker::HookModule: <" << name << "> hooked " << m_hookedAddrToName.size() - hookCountBefore << " functions" << endl;
}

typedef struct {
	FunctionSignature sig;
	int sigSize;
	int numBytesToCopy;
} PrologSignature;

static const PrologSignature prologSignatures[] = {
	{
		{
			NULL,

			"8B FF"					// mov edi, edi
			"55"					// push ebp
			"8B EC",				// mov ebp, esp
		},

		5,
		5,
	},
	{
		{
			NULL,

			"6A ??"					// push ??h
			"68 ?? ?? ?? ??"		// push offset dword_????????
			"E8 ?? ?? ?? ??",		// call __SEH_prolog
		},

		12,
		7,
	},
	{
		{
			NULL,

			"68 ?? ?? ?? ??"		// push ???h
			"68 ?? ?? ?? ??"		// push offset dword_????????
			"E8 ?? ?? ?? ??",		// call __SEH_prolog
		},

		15,
		5,
	},
	{
		{
			NULL,

			"FF 25 ?? ?? ?? ??"		// jmp ds:__imp__*
		},

		6,
		6,
	},
	{
		{
			NULL,

			"33 C0"		// xor eax, eax
			"50"		// push eax
			"50"		// push eax
			"6A ??"		// push ??
		},

		6,
		6,
	},
};

bool
CHooker::HookFunction(const string &name, void *address)
{
	const PrologSignature *prologSig = NULL;

	for (int i = 0; i < sizeof(prologSignatures) / sizeof(PrologSignature); i++)
	{
		const PrologSignature *ps = &prologSignatures[i];
		void *match_address;
		DWORD numMatches;
		char *error;

		if (!find_signature_in_range(&ps->sig, address, ps->sigSize,
									 &match_address, &numMatches, &error))
		{
			cerr << "failed to hook function " << name << " at " << address
				 << " because find_signature_in_range failed: " << error << endl;
			return false;
		}

		if (numMatches == 1)
		{
			prologSig = ps;
			break;
		}
	}

	if (prologSig == NULL)
	{
		cerr << "failed to hook function " << name << " at " << address
			 << " because its signature didn't match any of the supported ones" << endl;
		return false;
	}

	m_hookedAddrToName[address] = name;


	//
	// Generate the per-function stage 1 proxy which will have the following
    // layout:
	//
	// [code1]   <-- fixed size
	// [data]    <-- fixed size
	// [code2]   <-- variable size (depending on function)
	//
	// Where code1 is where it all starts.  The intercepted function will JMP
	// to it and it will "call" stage 2 -- see comment below for more info.
	//

	unsigned char *proxy = (unsigned char *) ospy_malloc(1 + sizeof(DWORD) +
														 sizeof(DWORD) +
													     prologSig->numBytesToCopy + 1 + sizeof(DWORD));
	int offset = 0;

	//
	// code1
	//

	// call stage 2
	// we use call here so that we get the address of proxy.data onto the top
	// of the stack, to make the per-function proxy as small as possible.
	proxy[offset++] = 0xe8;
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
	memcpy(proxy + offset, address, prologSig->numBytesToCopy);
	offset += prologSig->numBytesToCopy;

	// and finally a JMP to the next instruction in the original
	proxy[offset++] = 0xe9;
	*((DWORD *) (proxy + offset)) = ((DWORD) address + prologSig->numBytesToCopy) - (DWORD) (proxy + offset + 4);
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
		// STEP 1: store the proxy.data's address somewhere safe and save all
		// registers that have to be left unclobbered.
		//

		mov ecx, [esp];
		add esp, 4;

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
		//      space for temporary storage when it's done with them (or, the
		//      code might intentionally alter them, ie. incrementing pointers),
		//      so we make a copy of the whole thing so that the function won't
		//      clobber the original arguments.
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

__declspec (naked) void
CHooker::PreExecRedirProxy(void *callerAddress)
{
	void *args, *retAddr;
	DWORD lastError;

	__asm {
		// Do the prolog dance
		push ebp;
		mov ebp, esp;
		sub esp, __LOCAL_SIZE;

		// Return address
		lea ecx, [ebp + 4 + 4 + 4];
		mov edx, [ecx];
		mov [retAddr], edx;

		// Arguments
		add ecx, 4;
		mov [args], ecx;

		pushad;
	}

	lastError = GetLastError();

	self()->PreExecProxy(callerAddress, retAddr, args, lastError);

	SetLastError(lastError);

	__asm {
		popad;

		// And the epilog dance
		mov esp, ebp;
		pop ebp;

		ret;
	}
}

__declspec (naked) void
CHooker::Stage3Proxy()
{
	void *callerAddress, *retAddr, *args;
	DWORD argsSize, retval, lastError;

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

		pushad;
	}

	lastError = GetLastError();

	CHooker::self()->PostExecProxy(callerAddress, retAddr, args, argsSize, retval, lastError);

	SetLastError(lastError);

	__asm {
		popad;

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
CHooker::PreExecProxy(void *callerAddress, void *retAddr, void *args, DWORD lastError)
{
	cout << "PreExecProxy: " << m_hookedAddrToName[callerAddress]
		<< " (" <<  callerAddress << ") called with retAddr=" << retAddr
		<< " and lastError=" << lastError << endl;

	cout << "PreExecProxy: 16 first bytes of args:" << endl << hexdump(args, 16, 16) << endl;
}

void
CHooker::PostExecProxy(void *callerAddress, void *retAddr, void *args, DWORD argsSize, DWORD &retval, DWORD &lastError)
{
	cout << "PostExecProxy: " << m_hookedAddrToName[callerAddress]
		<< " (" <<  callerAddress << ") called with retAddr=" << retAddr
		<< ", argsSize=" << argsSize << ", retval=" << (int) retval
		<< " and lastError=" << lastError << endl;

	if (argsSize > 0)
		cout << "PostExecProxy: the arguments (" << argsSize << " bytes)" << endl << hexdump(args, argsSize, 16) << endl;
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
