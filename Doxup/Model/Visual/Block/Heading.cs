using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Doxup.Model
{
    class Heading : IVisualContainer, IBlock
    {
        public int Level { get; set; } = 1;
        public string Id { get; set; }
        public List<IVisual> Children { get; }

        public Heading()
        {
            Children = new List<IVisual>();
        }
        public Heading(params IVisual[] elements)
        {
            Children = elements.ToList();
        }

        public Heading(IEnumerable<IVisual> elements)
        {
            Children = elements.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("h" + Level.ToString());
            if (!string.IsNullOrEmpty(Id))
                writer.WriteAttributeString("id", Id);
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
