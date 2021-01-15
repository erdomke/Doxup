using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Doxup.Model
{
    [DebuggerDisplay("{Text}")]
    class TextRun : IInline
    {
        public string Text { get; set; }

        public List<IVisual> Children => throw new NotImplementedException();

        public TextRun(string text)
        {
            Text = text;
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteString(Text);
        }

        internal static string RenderText(IVisual element)
        {
            var builder = new StringBuilder();
            RenderText(element, builder);
            return builder.ToString();
        }

        internal static string RenderText(IEnumerable<IVisual> elements)
        {
            var builder = new StringBuilder();
            foreach (var element in elements)
                RenderText(element, builder);
            return builder.ToString();
        }

        private static void RenderText(IVisual element, StringBuilder builder)
        {
            if (element is IVisualContainer container)
            {
                foreach (var child in container.Children)
                    RenderText(child, builder);
            }
            else if (element is TextRun text)
            {
                builder.Append(text.Text);
            }
            else if (element is Reference reference)
            {
                builder.Append(reference.Text ?? reference.Id);
            }
        }
    }
}
