using System;
using oSpy.Parser;
using oSpy.Util;
using oSpy.Net;
namespace oSpy
{
    interface ITransactionFactory
    {
        bool HandleSession(IPSession session);
        DebugLogger Logger { get; }
        string Name();
    }
}
