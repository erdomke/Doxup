using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Doxup.Model
{
    class ListBlock : IBlock
    {
        public bool Ordered { get; }

        public ListBlock(bool ordered)
        {
            Ordered = ordered;
        }

        public List<List<IVisual>> Items { get; } = new List<List<IVisual>>();

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement(Ordered ? "ol" : "ul");
            foreach (var item in Items)
            {
                writer.WriteStartElement("li");
                foreach (var child in item)
                    child.WriteTo(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
