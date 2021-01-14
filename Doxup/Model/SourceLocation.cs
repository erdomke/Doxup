using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Nudox.Model
{
    class SourceLocation : ISerializable
    {
        public string Path { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("source");
            writer.WriteAttributeString("path", Path);
            writer.WriteAttributeString("line", Line.ToString());
            writer.WriteAttributeString("column", Column.ToString());
            writer.WriteEndElement();
        }
    }
}
