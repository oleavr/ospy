#pragma abi native

struct SecBuffer {
    u32 cbBuffer;
    u32 BufferType;
    u8 *pvBuffer;
};

struct SecBufferDesc {
    u32 ulVersion;
    u32 cBuffers;
    u8 *pBuffers;
};
