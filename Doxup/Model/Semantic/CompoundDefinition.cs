using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Doxup.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class CompoundDefinition : ISerializable
    {
        public AccessModifier Access { get; set; }
        public string Namespace { get; set; }
        /*
 <xsd:enumeration value="class" />
<xsd:enumeration value="struct" />
<xsd:enumeration value="union" />
<xsd:enumeration value="interface" />
<xsd:enumeration value="protocol" />
<xsd:enumeration value="category" />
<xsd:enumeration value="exception" />
<xsd:enumeration value="service" />
<xsd:enumeration value="singleton" />
<xsd:enumeration value="module" />
<xsd:enumeration value="type" />
<xsd:enumeration value="group" />
<xsd:enumeration value="page" />
<xsd:enumeration value="example" />
<xsd:enumeration value="dir" />
         */
        public string Kind { get; set; }
        public string Id { get; set; }
        public Language Language { get; set; }
        public List<IVisual> Title { get; } = new List<IVisual>();
        public List<IVisual> Summary { get; } = new List<IVisual>();
        public List<IVisual> Documentation { get; } = new List<IVisual>();
        public List<MemberDefinition> Members { get; } = new List<MemberDefinition>();
        public List<Inherits> Inherits { get; } = new List<Inherits>();
        public SourceLocation SourceLocation { get; set; }

        private string DebuggerDisplay => $"{Access} {TextRun.RenderText(Title)}";

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("compound");
            if (!string.IsNullOrEmpty(Kind))
                writer.WriteAttributeString("kind", Kind);
            if (Access != AccessModifier.NotApplicable)
                writer.WriteAttributeString("access", Access.ToString());
            if (!string.IsNullOrEmpty(Namespace))
                writer.WriteAttributeString("namespace", Namespace);
            if (!string.IsNullOrEmpty(Language.Name))
                writer.WriteAttributeString("language", Language.Name);
            if (!string.IsNullOrEmpty(Id))
                writer.WriteAttributeString("id", Id);
            
            writer.WriteStartElement("name");
            foreach (var child in Title)
                child.WriteTo(writer);
            writer.WriteEndElement();

            foreach (var inherits in Inherits)
                inherits.WriteTo(writer);

            if (Summary.Count > 0)
            {
                writer.WriteStartElement("summary");
                foreach (var child in Summary)
                    child.WriteTo(writer);
                writer.WriteEndElement();
            }
            foreach (var child in Documentation)
                child.WriteTo(writer);
            foreach (var child in Members)
                child.WriteTo(writer);
            SourceLocation?.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
