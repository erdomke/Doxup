using System.Xml;

namespace Doxup.Model
{
    class Image : IVisual
    {
        public string AltText { get; set; }
        public string Src { get; set; }

        public void WriteTo(XmlWriter writer)
        {
            writer.WriteStartElement("img");
            writer.WriteAttributeString("src", Src);
            writer.WriteAttributeString("alt", AltText);
            writer.WriteEndElement();
        }
    }
}
