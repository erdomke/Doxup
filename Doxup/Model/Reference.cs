using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Nudox.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Reference : IElement
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Text { get; set; }

        public List<IElement> Children => throw new NotImplementedException();

        private string DebuggerDisplay => Text ?? Id;

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("see");
            writer.WriteAttributeString("cref", Id);
            if (!string.IsNullOrEmpty(Kind))
                writer.WriteAttributeString("kind", Kind);
            if (!string.IsNullOrEmpty(Text))
                writer.WriteString(Text);
            writer.WriteEndElement();
        }
    }
}
