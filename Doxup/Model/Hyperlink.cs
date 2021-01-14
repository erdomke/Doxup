using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Nudox.Model
{
    internal class Hyperlink : IContainerElement
    {
        public string Href { get; }
        public List<IElement> Children { get; }

        public Hyperlink(string href, IEnumerable<IElement> children)
        {
            Href = href;
            Children = children.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("a");
            writer.WriteAttributeString("href", Href);
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
