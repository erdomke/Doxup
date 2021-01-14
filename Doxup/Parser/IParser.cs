using Nudox.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nudox.Parser
{
    interface IParser
    {
        Project Parse(IEnumerable<string> paths);
    }
}
