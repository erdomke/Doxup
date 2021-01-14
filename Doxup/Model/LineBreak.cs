using System.Xml;

namespace Nudox.Model
{
    class LineBreak : IElement
    {
        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("br");
            writer.WriteEndElement();
        }
    }
}
