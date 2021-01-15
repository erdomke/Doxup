using System;
using System.Collections.Generic;
using System.Text;

namespace Doxup.Model
{
    class Project
    {
        public List<Page> Pages { get; } = new List<Page>();
        public List<CompoundDefinition> Definitions { get; } = new List<CompoundDefinition>();
    }
}
