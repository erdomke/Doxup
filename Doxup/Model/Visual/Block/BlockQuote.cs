using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Doxup.Model
{
    class BlockQuote : IVisualContainer, IBlock
    {
        public List<IVisual> Children { get; }

        public BlockQuote()
        {
            Children = new List<IVisual>();
        }
        public BlockQuote(params IVisual[] elements)
        {
            Children = elements.ToList();
        }

        public BlockQuote(IEnumerable<IVisual> elements)
        {
            Children = elements.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("blockquote");
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
