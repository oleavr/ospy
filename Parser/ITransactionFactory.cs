using System;
using oSpy.Parser;
using oSpy.Util;
namespace oSpy
{
    interface ITransactionFactory
    {
        bool HandleSession(IPSession session);
        DebugLogger Logger { get; }
        string Name();
    }
}
