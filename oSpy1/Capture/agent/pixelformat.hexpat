#pragma abi native

enum PixelType : u8 {
    RGBA = 0,
    ColorIndex = 1,
};

enum LayerType : u8 {
    MainPlane = 0,
    OverlayPlane = 1,
    UnderlayPlane = 255,
};

bitfield PixelFormatFlags {
    DoubleBuffer : 1;
    Stereo : 1;
    DrawToWindow : 1;
    DrawToBitmap : 1;
    SupportGdi : 1;
    SupportOpenGL : 1;
    GenericFormat : 1;
    NeedPalette : 1;
    NeedSystemPalette : 1;
    SwapExchange : 1;
    SwapCopy : 1;
    SwapLayerBuffers : 1;
    GenericAccelerated : 1;
    SupportDirectDraw : 1;
    Direct3DAccelerated : 1;
    SupportComposition : 1;
    padding : 16;
};

struct PIXELFORMATDESCRIPTOR {
    u16 nSize;
    u16 nVersion;
    PixelFormatFlags dwFlags;
    PixelType iPixelType;
    u8 cColorBits;
    u8 cRedBits;
    u8 cRedShift;
    u8 cGreenBits;
    u8 cGreenShift;
    u8 cBlueBits;
    u8 cBlueShift;
    u8 cAlphaBits;
    u8 cAlphaShift;
    u8 cAccumBits;
    u8 cAccumRedBits;
    u8 cAccumGreenBits;
    u8 cAccumBlueBits;
    u8 cAccumAlphaBits;
    u8 cDepthBits;
    u8 cStencilBits;
    u8 cAuxBuffers;
    LayerType iLayerType;
    u8 bReserved;
    u32 dwLayerMask;
    u32 dwVisibleMask;
    u32 dwDamageMask;
};
