using System;
using System.Collections.Generic;
using System.Text;

namespace oSpy.Util
{
    public interface DebugLogger
    {
        void AddMessage(string msg);
        void AddMessage(string msg, params object[] vals);
    }
}
