using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Doxup.Model
{
    class CodeBlock : IVisualContainer, IBlock
    {
        public Language Language { get; set; }

        public List<IVisual> Children { get; }

        public CodeBlock()
        {
            Children = new List<IVisual>();
        }
        public CodeBlock(params IVisual[] elements)
        {
            Children = elements.ToList();
        }

        public CodeBlock(IEnumerable<IVisual> elements)
        {
            Children = elements.ToList();
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("pre");
            if (!string.IsNullOrEmpty(Language.Name))
                writer.WriteAttributeString("language", Language.Name);
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
