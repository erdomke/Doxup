using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Nudox.Model
{
    class Table : IElement
    {
        public bool DefinitionList { get; set; }
        public Row Header { get; set; }
        public List<Row> Rows { get; } = new List<Row>();

        public void WriteTo(XmlWriter writer)
        {
            if (DefinitionList)
            {
                writer.WriteStartElement("list");
                writer.WriteAttributeString("type", "table");
                if (Header != null)
                {
                    writer.WriteStartElement("listheader");
                    writer.WriteStartElement("term");
                    foreach (var child in Header[0])
                        child.WriteTo(writer);
                    writer.WriteEndElement();
                    writer.WriteStartElement("description");
                    foreach (var child in Header[1])
                        child.WriteTo(writer);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                foreach (var row in Rows)
                {
                    writer.WriteStartElement("listheader");
                    writer.WriteStartElement("term");
                    foreach (var child in row[0])
                        child.WriteTo(writer);
                    writer.WriteEndElement();
                    writer.WriteStartElement("description");
                    foreach (var child in row[1])
                        child.WriteTo(writer);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteStartElement("table");
                if (Header != null)
                {
                    writer.WriteStartElement("thead");
                    writer.WriteStartElement("tr");
                    foreach (var cell in Header)
                    {
                        writer.WriteStartElement("th");
                        foreach (var child in cell)
                            child.WriteTo(writer);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                foreach (var row in Rows)
                {
                    writer.WriteStartElement("tr");
                    foreach (var cell in row)
                    {
                        writer.WriteStartElement("td");
                        foreach (var child in cell)
                            child.WriteTo(writer);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }
    }
}
