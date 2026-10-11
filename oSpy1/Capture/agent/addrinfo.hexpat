#pragma abi native

struct addrinfo {
    s32 ai_flags;
    s32 ai_family;
    s32 ai_socktype;
    s32 ai_protocol;
    u8 *ai_addrlen;
    char *ai_canonname;
    u8 *ai_addr;
    addrinfo *ai_next;
};
