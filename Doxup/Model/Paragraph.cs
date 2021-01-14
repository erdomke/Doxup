using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Nudox.Model
{
    class Paragraph : IContainerElement
    {
        public List<IElement> Children { get; }

        public Paragraph()
        {
            Children = new List<IElement>();
        }
        public Paragraph(params IElement[] elements)
        {
            Children = elements.ToList();
        }

        public Paragraph(IEnumerable<IElement> elements)
        {
            Children = elements.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("para");
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
