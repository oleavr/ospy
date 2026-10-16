#pragma abi native

struct OVERLAPPED_ENTRY {
    char *lpCompletionKey;
    char *lpOverlapped;
    char *internal;
    u32 dwNumberOfBytesTransferred;
};
