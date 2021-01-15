using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Doxup.Model
{
    class Highlight : IVisualContainer, IInline
    {
        public HighlightStyle Style { get; set; }
        public List<IVisual> Children { get; }

        public Highlight(HighlightStyle style, IEnumerable<IVisual> children)
        {
            Style = style;
            Children = children.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("hl");
            writer.WriteAttributeString("class", Style.ToString().ToLowerInvariant());
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
