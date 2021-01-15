using System;
using System.Collections.Generic;
using System.Text;

namespace Doxup.Model
{
    class Row : List<List<IVisual>>
    {
        public Row() : base() { }
        public Row(IEnumerable<List<IVisual>> cells) : base(cells) { }
    }
}
