using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace Nudox.Model
{
    [DebuggerDisplay("{Text}")]
    class TextRun : IElement
    {
        public string Text { get; }

        public List<IElement> Children => throw new NotImplementedException();

        public TextRun(string text)
        {
            Text = text;
        }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteString(Text);
        }

        internal static string RenderText(IElement element)
        {
            var builder = new StringBuilder();
            RenderText(element, builder);
            return builder.ToString();
        }

        internal static string RenderText(IEnumerable<IElement> elements)
        {
            var builder = new StringBuilder();
            foreach (var element in elements)
                RenderText(element, builder);
            return builder.ToString();
        }

        private static void RenderText(IElement element, StringBuilder builder)
        {
            if (element is IContainerElement container)
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
