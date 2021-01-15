using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Doxup.Model
{
    class Paragraph : IVisualContainer, IBlock
    {
        public List<IVisual> Children { get; }

        public Paragraph()
        {
            Children = new List<IVisual>();
        }
        public Paragraph(params IVisual[] elements)
        {
            Children = elements.ToList();
        }

        public Paragraph(IEnumerable<IVisual> elements)
        {
            Children = elements.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("p");
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
