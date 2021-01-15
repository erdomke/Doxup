using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Doxup.Model
{
    internal class StyledSpan : IVisualContainer, IInline
    {
        public InlineStyle Style { get; }
        public List<IVisual> Children { get; }

        public StyledSpan(InlineStyle style, IEnumerable<IVisual> children)
        {
            Style = style;
            Children = children.ToList();
        }

        public static bool TryGetStyle(string tagName, out InlineStyle style)
        {
            switch (tagName)
            {
                case "b":
                case "bold":
                case "strong":
                    style = InlineStyle.Strong;
                    return true;
                case "c":
                case "computeroutput":
                case "samp":
                    style = InlineStyle.ComputerCode;
                    return true;
                case "del":
                    style = InlineStyle.Delete;
                    return true;
                case "em":
                case "emphasis":
                case "i":
                    style = InlineStyle.Emphasis;
                    return true;
                case "ins":
                    style = InlineStyle.Insert;
                    return true;
                case "s":
                case "strike":
                    style = InlineStyle.Strikethrough;
                    return true;
                case "small":
                    style = InlineStyle.Small;
                    return true;
                case "sub":
                case "subscript":
                    style = InlineStyle.Subscript;
                    return true;
                case "sup":
                case "superscript":
                    style = InlineStyle.Superscript;
                    return true;
                case "underline":
                    style = InlineStyle.Underline;
                    return true;
            }
            style = InlineStyle.Strong;
            return false;
        }

        public void WriteTo(XmlWriter writer)
        {
            switch (Style)
            {
                case InlineStyle.ComputerCode:
                    writer.WriteStartElement("c");
                    break;
                case InlineStyle.Delete:
                    writer.WriteStartElement("del");
                    break;
                case InlineStyle.Emphasis:
                    writer.WriteStartElement("em");
                    break;
                case InlineStyle.Insert:
                    writer.WriteStartElement("ins");
                    break;
                case InlineStyle.Small:
                    writer.WriteStartElement("small");
                    break;
                case InlineStyle.Strikethrough:
                    writer.WriteStartElement("s");
                    break;
                case InlineStyle.Strong:
                    writer.WriteStartElement("strong");
                    break;
                case InlineStyle.Subscript:
                    writer.WriteStartElement("sub");
                    break;
                case InlineStyle.Superscript:
                    writer.WriteStartElement("sup");
                    break;
                case InlineStyle.Underline:
                    writer.WriteStartElement("u");
                    break;
            }
            foreach (var child in Children)
                child.WriteTo(writer);
            writer.WriteEndElement();
        }
    }
}
