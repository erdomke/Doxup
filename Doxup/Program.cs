using System;
using System.Linq;
using System.Xml;

namespace Doxup
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser.DoxygenXmlParser();
            var files = new[]
            {
                @"C:\Users\erdomke\source\GitHub\Innovator.Client\doc\xml\md__test.xml",
                @"C:\Users\erdomke\source\GitHub\Innovator.Client\doc\xml\class_innovator_1_1_client_1_1_connection_1_1_aras_http_connection.xml"
            };
            var project = parser.Parse(files);
            using (var writer = XmlWriter.Create(@"C:\Users\erdomke\source\GitHub\Innovator.Client\doc\convert.xml", new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "  "
            }))
            {
                project.Pages.First().WriteTo(writer);
            }
        }
    }
}
