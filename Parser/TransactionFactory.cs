using System;
using System.Collections.Generic;
using System.Text;
using oSpy.Util;
using oSpy.Net;
namespace oSpy.Parser
{
    public abstract class TransactionFactory : ITransactionFactory
    {
        protected DebugLogger logger;
        public DebugLogger Logger
        {
            get { return logger; }
        }

        public TransactionFactory(DebugLogger logger)
        {
            this.logger = logger;
        }

        public abstract bool HandleSession(IPSession session);
        /*
         * Used to identify this factory for configuration purposes
         */
        public abstract string Name();
    }
}
