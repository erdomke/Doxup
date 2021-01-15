using System.Xml;

namespace Doxup.Model
{
    class HorizontalRule : IBlock
    {
        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("hr");
            writer.WriteEndElement();
        }
    }
}
