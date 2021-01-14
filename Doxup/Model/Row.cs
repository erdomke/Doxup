using System;
using System.Collections.Generic;
using System.Text;

namespace Nudox.Model
{
    class Row : List<List<IElement>>
    {
        public Row() : base() { }
        public Row(IEnumerable<List<IElement>> cells) : base(cells) { }
    }
}
