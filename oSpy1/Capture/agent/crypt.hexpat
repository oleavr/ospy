#pragma abi native

enum AlgId : u32 {
    MD2 = 0x8001,
    MD4 = 0x8002,
    MD5 = 0x8003,
    SHA1 = 0x8004,
    MAC = 0x8005,
    HMAC = 0x8009,
    SHA_256 = 0x800c,
    SHA_384 = 0x800d,
    SHA_512 = 0x800e,
    RSA_SIGN = 0x2400,
    RSA_KEYX = 0xa400,
    DES = 0x6601,
    TRIPLE_DES = 0x6603,
    RC2 = 0x6602,
    RC4 = 0x6801,
    AES_128 = 0x660e,
    AES_192 = 0x660f,
    AES_256 = 0x6610,
    DH_SF = 0xaa01,
    DH_EPHEM = 0xaa02,
};

enum KeyParam : u32 {
    IV = 1,
    Padding = 3,
    Mode = 4,
    ModeBits = 5,
    Permissions = 6,
    AlgId = 7,
    BlockLen = 8,
};

enum HashParam : u32 {
    AlgId = 1,
    HashVal = 2,
    HashSize = 4,
    HmacInfo = 5,
};

struct HMAC_INFO {
    AlgId HashAlgid;
    u8 *pbInnerString;
    u32 cbInnerString;
    u8 *pbOuterString;
    u32 cbOuterString;
};
