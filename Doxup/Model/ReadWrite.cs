using System;
using System.Collections.Generic;
using System.Text;

namespace Nudox.Model
{
    [Flags]
    enum ReadWrite
    {
        NotApplicable = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write
    }
}
