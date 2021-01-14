using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Nudox.Model
{
    class InlineCode : IContainerElement
    {
        public List<IElement> Children { get; }

        public InlineCode(IEnumerable<IElement> children)
        {
            Children = children.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("c");
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
