using System;
using System.Collections.Generic;
using System.Text;

namespace Doxup.Model
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
