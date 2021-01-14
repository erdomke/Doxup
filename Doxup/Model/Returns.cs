using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Nudox.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Returns : IContainerElement
    {
        public List<IElement> Children { get; } = new List<IElement>();
        public int? Code { get; set; }
        public TypeReference Type { get; } = new TypeReference();

        private string DebuggerDisplay => $"returns {TextRun.RenderText(Type)} : {TextRun.RenderText(Children)}";

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("returns");
            if (Code.HasValue)
                writer.WriteAttributeString("code", Code.Value.ToString());
            Type.WriteTo(writer);
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
