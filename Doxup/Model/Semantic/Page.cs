using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Doxup.Model
{
    class Page : ISerializable
    {
        public List<IVisual> Documentation { get; } = new List<IVisual>();

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("main");
            foreach (var child in Documentation)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
