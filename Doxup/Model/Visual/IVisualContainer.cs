using System.Collections.Generic;

namespace Doxup.Model
{
    interface IVisualContainer : IVisual
    {
        List<IVisual> Children { get; }
    }
}
