#pragma abi native

struct sockaddr_in {
    u16 sin_family;
    be u16 sin_port;
    u8 sin_addr[4];
    u8 sin_zero[8];
};
