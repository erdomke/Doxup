using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Nudox.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class Parameter : ISerializable
    {
        public string DefaultValue { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public TypeReference Type { get; } = new TypeReference();

        public List<IElement> Documentation { get; } = new List<IElement>();

        private string DebuggerDisplay => $"{TextRun.RenderText(Type)} {Name} = {DefaultValue}";

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", Name);
            if (!string.IsNullOrEmpty(DefaultValue))
                writer.WriteAttributeString("defaultvalue", DefaultValue);
            if (!string.IsNullOrEmpty(Location))
                writer.WriteAttributeString("location", Location);
            Type.WriteTo(writer);
            foreach (var child in Documentation)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
