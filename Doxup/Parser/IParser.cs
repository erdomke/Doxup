using Doxup.Model;
using System.Collections.Generic;

namespace Doxup.Parser
{
    interface IParser
    {
        Project Parse(IEnumerable<string> paths);
    }
}
