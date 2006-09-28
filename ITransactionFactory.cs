using System;
namespace oSpy
{
    interface ITransactionFactory
    {
        bool HandleSession(IPSession session);
        DebugLogger Logger { get; }
        string Name();
    }
}
