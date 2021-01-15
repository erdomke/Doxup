using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Doxup.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Reference : IInline
    {
        public string Id { get; set; }
        public string Kind { get; set; }
        public string Text { get; set; }

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
