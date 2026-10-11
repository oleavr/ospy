#pragma abi native

enum HttpVerb : s32 {
    Unparsed = 0,
    Unknown = 1,
    Invalid = 2,
    OPTIONS = 3,
    GET = 4,
    HEAD = 5,
    POST = 6,
    PUT = 7,
    DELETE = 8,
    TRACE = 9,
    CONNECT = 10,
    TRACK = 11,
    MOVE = 12,
    COPY = 13,
    PROPFIND = 14,
    PROPPATCH = 15,
    MKCOL = 16,
    LOCK = 17,
    UNLOCK = 18,
    SEARCH = 19,
};

enum HttpDataChunkType : s32 {
    FromMemory = 0,
    FromFileHandle = 1,
    FromFragmentCache = 2,
    FromFragmentCacheEx = 3,
    Trailers = 4,
};

struct HTTP_VERSION {
    u16 MajorVersion;
    u16 MinorVersion;
};

struct HTTP_KNOWN_HEADER {
    u16 RawValueLength;
    char *pRawValue;
};

struct HTTP_UNKNOWN_HEADER {
    u16 NameLength;
    u16 RawValueLength;
    char *pName;
    char *pRawValue;
};

struct HTTP_COOKED_URL {
    u16 FullUrlLength;
    u16 HostLength;
    u16 AbsPathLength;
    u16 QueryStringLength;
    u8 *pFullUrl;
    u8 *pHost;
    u8 *pAbsPath;
    u8 *pQueryString;
};

struct HTTP_TRANSPORT_ADDRESS {
    u8 *pRemoteAddress;
    u8 *pLocalAddress;
};

struct HTTP_REQUEST_HEADERS {
    u16 UnknownHeaderCount;
    u8 *pUnknownHeaders;
    u16 TrailerCount;
    u8 *pTrailers;
    HTTP_KNOWN_HEADER KnownHeaders[41];
};

struct HTTP_RESPONSE_HEADERS {
    u16 UnknownHeaderCount;
    u8 *pUnknownHeaders;
    u16 TrailerCount;
    u8 *pTrailers;
    HTTP_KNOWN_HEADER KnownHeaders[30];
};

struct HTTP_REQUEST {
    u32 Flags;
    u64 ConnectionId;
    u64 RequestId;
    u64 UrlContext;
    HTTP_VERSION Version;
    HttpVerb Verb;
    u16 UnknownVerbLength;
    u16 RawUrlLength;
    char *pUnknownVerb;
    char *pRawUrl;
    HTTP_COOKED_URL CookedUrl;
    HTTP_TRANSPORT_ADDRESS Address;
    HTTP_REQUEST_HEADERS Headers;
};

struct HTTP_RESPONSE {
    u32 Flags;
    HTTP_VERSION Version;
    u16 StatusCode;
    u16 ReasonLength;
    char *pReason;
    HTTP_RESPONSE_HEADERS Headers;
};

struct HTTP_DATA_CHUNK {
    HttpDataChunkType DataChunkType;
    u8 *pBuffer;
    u32 BufferLength;
};
