using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Doxup.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class MemberDefinition : ISerializable
    {
        public AccessModifier Access { get; set; }
        public string Id { get; set; }
        public bool? IsStatic { get; set; }
        public string Kind { get; set; }
        public string Location { get; set; }
        public ReadWrite ReadWrite { get; set; }
        public List<IVisual> InitialValue { get; } = new List<IVisual>();

        public List<IVisual> Title { get; } = new List<IVisual>();
        public List<IVisual> Summary { get; } = new List<IVisual>();
        public List<IVisual> Documentation { get; } = new List<IVisual>();
        public List<Parameter> Parameters { get; } = new List<Parameter>();
        public SourceLocation SourceLocation { get; set; }

        private string DebuggerDisplay => $"{Access} {Kind} {TextRun.RenderText(Title)}";

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("member");
            if (!string.IsNullOrEmpty(Kind))
                writer.WriteAttributeString("kind", Kind);
            if (Access != AccessModifier.NotApplicable)
                writer.WriteAttributeString("access", Access.ToString());
            if (IsStatic.HasValue)
                writer.WriteAttributeString("static", IsStatic.Value ? "yes" : "no");
            if (!string.IsNullOrEmpty(Location))
                writer.WriteAttributeString("location", Location);
            if (ReadWrite != ReadWrite.NotApplicable)
                writer.WriteAttributeString("readwrite", ReadWrite.ToString());
            if (!string.IsNullOrEmpty(Id))
                writer.WriteAttributeString("id", Id);

            writer.WriteStartElement("name");
            foreach (var child in Title)
                child.WriteTo(writer);
            writer.WriteEndElement();

            if (Summary.Count > 0)
            {
                writer.WriteStartElement("summary");
                foreach (var child in Summary)
                    child.WriteTo(writer);
                writer.WriteEndElement();
            }
            foreach (var child in Parameters)
                child.WriteTo(writer);
            if (InitialValue.Count > 0)
            {
                writer.WriteStartElement("initial");
                foreach (var child in InitialValue)
                    child.WriteTo(writer);
                writer.WriteEndElement();
            }
            foreach (var child in Documentation)
                child.WriteTo(writer);
            SourceLocation?.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
