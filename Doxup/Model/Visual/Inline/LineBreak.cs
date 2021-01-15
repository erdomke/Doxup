using System.Xml;

namespace Doxup.Model
{
    class LineBreak : IInline
    {
        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("br");
            writer.WriteEndElement();
        }
    }
}
