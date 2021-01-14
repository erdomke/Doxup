using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Nudox.Model
{
    /* TODO:
     * - Inheritance
     * - Fix explicity interface imlementations
     * - <remarks>
     * - <exceptions>
     */

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class CompoundDefinition : ISerializable
    {
        public AccessModifier Access { get; set; }
        public string Namespace { get; set; }
        public string Kind { get; set; }
        public string Id { get; set; }
        public Language Language { get; set; }
        public List<IElement> Title { get; } = new List<IElement>();
        public List<IElement> Summary { get; } = new List<IElement>();
        public List<IElement> Documentation { get; } = new List<IElement>();
        public List<MemberDefinition> Members { get; } = new List<MemberDefinition>();
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

            var summary = Summary;
            if (summary.Count == 1 && summary[0] is Paragraph summaryParagraph)
                summary = summaryParagraph.Children;
            if (summary.Count > 0)
            {
                writer.WriteStartElement("summary");
                foreach (var child in summary)
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
