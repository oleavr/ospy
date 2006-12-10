// IPC.h

#pragma once

#define IPC_BLOCK_COUNT			512
#define IPC_BLOCK_SIZE			4096

#define IPC_MAX_ADDR			256

namespace oSpy {
namespace IPC {

	struct Block
	{
		// Variables
		LONG					Next;						// Next block in the circular linked list
		LONG					Prev;						// Previous block in the circular linked list

		volatile LONG			doneRead;					// Flag used to signal that this block has been read
		volatile LONG			doneWrite;					// Flag used to signal that this block has been written
		
		DWORD					Amount;						// Amount of data help in this block
		DWORD					_Padding;					// Padded used to ensure 64bit boundary

		BYTE					Data[IPC_BLOCK_SIZE];		// Data contained in this block
	};

	struct MemBuff
	{
		// Block data, this is placed first to remove the offset (optimisation)
		Block					m_Blocks[IPC_BLOCK_COUNT];	// Array of buffers that are used in the communication

		// Cursors
		volatile LONG			m_ReadEnd;					// End of the read cursor
		volatile LONG			m_ReadStart;				// Start of read cursor

		volatile LONG			m_WriteEnd;					// Pointer to the first write cursor, i.e. where we are currently writting to
		volatile LONG			m_WriteStart;				// Pointer in the list where we are currently writting
	};

	public ref class Server
	{
public:
		// Construct / Destruct
		Server(const System::String ^name);
		~Server();

private:
		// Internal variables
		char					*m_sAddr;		// Address of this server
		HANDLE					m_hMapFile;		// Handle to the mapped memory file
		HANDLE					m_hSignal;		// Event used to signal when data exists
		HANDLE					m_hAvail;		// Event used to signal when some blocks become available
		MemBuff					*m_pBuf;		// Buffer that points to the shared memory

public:
		// Exposed functions
		array<byte> ^ReadBlock(DWORD dwTimeout);

protected:
		// No need to expose these yet
		char*					getAddress(void) { return m_sAddr; };

		// Block functions
		Block*					getBlock(DWORD dwTimeout);
		void					retBlock(Block* pBlock);

		// Create and destroy functions
		void					create(const System::String ^name);
		void					close(void);
	};
}
}
