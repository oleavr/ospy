#pragma once

// Definitions
#define IPC_BLOCK_COUNT			512
#define IPC_BLOCK_SIZE			4096

#define IPC_MAX_ADDR			256


// ---------------------------------------
// -- Inter-Process Communication class --
// ---------------------------------------------------------------
// Provides intercommunication between processes and their threads
// ---------------------------------------------------------------
class IPCClient
{
public:
	// Construct / Destruct
	IPCClient(void);
	IPCClient(const char *name);
	~IPCClient();

	// Block that represents a piece of data to transmit between the
	// client and server
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

	// ID Generator
	static DWORD GetID(void)
	{
		// Generate an ID and return id
		static volatile LONG id = 1;
		return (DWORD)InterlockedIncrement((LONG*)&id);
	};

	// Exposed functions
	DWORD					write(void *pBuff, DWORD amount, DWORD dwTimeout = INFINITE);	// Writes to the buffer
	bool					waitAvailable(DWORD dwTimeout = INFINITE);						// Waits until some blocks become available

	Block*					getBlock(DWORD dwTimeout = INFINITE);							// Gets a block
	void					postBlock(Block *pBlock);										// Posts a block to be processed				

	// Functions
	BOOL					IsOk(void) { if (m_pBuf) return true; else return false; };

private:
	// Shared memory buffer that contains everything required to transmit
	// data between the client and server
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

	// Internal variables
	char					*m_sAddr;		// Address of this server
	HANDLE					m_hMapFile;		// Handle to the mapped memory file
	HANDLE					m_hSignal;		// Event used to signal when data exists
	HANDLE					m_hAvail;		// Event used to signal when some blocks become available
	MemBuff					*m_pBuf;		// Buffer that points to the shared memory
};
