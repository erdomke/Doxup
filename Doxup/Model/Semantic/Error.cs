using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Doxup.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Error : IVisualContainer
    {
        public List<IVisual> Children { get; } = new List<IVisual>();
        public int? Code { get; set; }
        public TypeReference Type { get; } = new TypeReference();

        private string DebuggerDisplay => $"exception {TextRun.RenderText(Type)} : {TextRun.RenderText(Children)}";

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("exception");
            if (Code.HasValue)
                writer.WriteAttributeString("code", Code.Value.ToString());
            Type.WriteTo(writer);
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
