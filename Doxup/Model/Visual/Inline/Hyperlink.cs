using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Doxup.Model
{
    internal class Hyperlink : IVisualContainer, IInline
    {
        public string Title { get; set; }
        public string Href { get; }
        public List<IVisual> Children { get; }

        public Hyperlink(string href, IEnumerable<IVisual> children)
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
