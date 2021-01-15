using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Doxup.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Inherits : ISerializable
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public List<Inherits> Children { get; } = new List<Inherits>();

        private string DebuggerDisplay => Text ?? Id;

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("inherits");
            if (!string.IsNullOrEmpty(Id))
                writer.WriteAttributeString("cref", Id);
            if (!string.IsNullOrEmpty(Text))
                writer.WriteAttributeString("text", Text);
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
