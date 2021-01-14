using System;
using System.Linq;
using System.Xml;

namespace Nudox
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser.DoxygenXmlParser();
            var project = parser.Parse(new[] { @"C:\Users\erdomke\source\GitHub\Innovator.Client\doc\xml\class_innovator_1_1_client_1_1_connection_1_1_aras_http_connection.xml" });
            using (var writer = XmlWriter.Create(@"C:\Users\erdomke\source\GitHub\Innovator.Client\doc\convert.xml", new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  "
            }))
            {
                project.Definitions.First().WriteTo(writer);
            }
        }
    }
}
