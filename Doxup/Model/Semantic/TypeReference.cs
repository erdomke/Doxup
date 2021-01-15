using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Doxup.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class TypeReference : List<IVisual>, ISerializable
    {
        public TypeReference() : base() { }
        public TypeReference(IEnumerable<IVisual> elements) : base(elements) { }

        private string DebuggerDisplay => TextRun.RenderText(this);

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("type");
            foreach (var child in this)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
