//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

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
        ///<summary>
        ///Used to identify this factory for configuration purposes
        ///</summary>
        public abstract string Name();
    }
}
