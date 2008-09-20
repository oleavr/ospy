/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *                     Frode Hus <husfro@gmail.com>
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.Text;
using oSpyClassic.Util;
using oSpyClassic.Net;

namespace oSpyClassic.Parser
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
