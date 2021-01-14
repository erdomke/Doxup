using System;
using System.Collections.Generic;
using System.Text;

namespace Nudox.Model
{
    interface IContainerElement : IElement
    {
        List<IElement> Children { get; }
    }
}
