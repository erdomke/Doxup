using Nudox.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Nudox.Parser
{
    class DoxygenXmlParser : IParser
    {
        public Project Parse(IEnumerable<string> paths)
        {
            var project = new Project();
            foreach (var path in paths)
            {
                var element = XElement.Load(path);
                var definition = element.Element("compounddef");
                var definitionType = (string)definition?.Attribute("kind") ?? "";
                switch (definitionType)
                {
                    case "file":
                    case "":
                        break;
                    case "namespace":
                        foreach (var enumDef in definition.Descendants("memberdef")
                            .Where(e => (string)e.Attribute("kind") == "enum"))
                        {
                            project.Definitions.Add(ParseEnum(definition, enumDef));
                        }
                        break;
                    default:
                        project.Definitions.Add(ParseCompound(definition));
                        break;
                }
            }
            return project;
        }

        private CompoundDefinition ParseEnum(XElement parent, XElement definition)
        {
            var result = new CompoundDefinition()
            {
                Access = GetAccess((string)definition.Attribute("prot")),
                Kind = (string)definition.Attribute("kind"),
                Id = (string)definition.Attribute("id"),
                Language = new Language((string)parent.Attribute("language")),
                SourceLocation = GetLocation(definition)
            };
            var nsName = ((string)parent.Element("compoundname")).Split("::");
            result.Namespace = result.Language == Language.Cpp
                ? string.Join("::", nsName)
                : string.Join(".", nsName);
            result.Title.Add(new TextRun((string)definition.Attribute("name")));
            result.Summary.AddRange(ParseParagraphs(definition.Element("briefdescription")));
            result.Members.AddRange(definition.Elements("enumvalue")
                .Select(e =>
                {
                    var member = new MemberDefinition()
                    {
                        Access = GetAccess((string)e.Attribute("prot")),
                        Kind = "enumvalue",
                        Id = (string)e.Attribute("id"),
                        SourceLocation = GetLocation(e)
                    };
                    member.Title.Add(new TextRun((string)e.Attribute("name")));
                    member.Summary.AddRange(ParseParagraphs(e.Element("briefdescription")));
                    return member;
                }));
            return result;
        }

        private CompoundDefinition ParseCompound(XElement definition)
        {
            var result = new CompoundDefinition()
            {
                Access = GetAccess((string)definition.Attribute("prot")),
                Kind = (string)definition.Attribute("kind"),
                Id = (string)definition.Attribute("id"),
                Language = new Language((string)definition.Attribute("language")),
                SourceLocation = GetLocation(definition)
            };
            var name = ((string)definition.Element("compoundname")).Split("::");
            result.Namespace = result.Language == Language.Cpp
                ? string.Join("::", name, 0, name.Length - 1)
                : string.Join(".", name, 0, name.Length - 1);
            result.Title.Add(new TextRun(name.Last()));
            result.Summary.AddRange(ParseParagraphs(definition.Element("briefdescription")));
            result.Members.AddRange(definition.Elements("sectiondef").Elements("memberdef").Select(e => ParseMember(result.Language, e)));
            return result;
        }

        private MemberDefinition ParseMember(Language language, XElement definition)
        {
            var result = new MemberDefinition()
            {
                Access = GetAccess((string)definition.Attribute("prot")),
                Kind = (string)definition.Attribute("kind"),
                Id = (string)definition.Attribute("id"),
                IsStatic = (string)definition.Attribute("static") == "yes",
                SourceLocation = GetLocation(definition)
            };
            result.Title.Add(new TextRun((string)definition.Element("name")));
            result.Summary.AddRange(ParseParagraphs(definition.Element("briefdescription")));

            if ((string)definition.Attribute("readable") == "yes"
                || (string)definition.Attribute("gettable") == "yes")
            {
                result.ReadWrite |= ReadWrite.Read;
            }
            if ((string)definition.Attribute("writable") == "yes"
                || (string)definition.Attribute("settable") == "yes")
            {
                result.ReadWrite |= ReadWrite.Write;
            }

            if (result.ReadWrite == ReadWrite.NotApplicable && result.Kind == "variable")
                result.ReadWrite = ReadWrite.ReadWrite;

            var typeChildren = new TypeReference(ParseFormattingChildren(definition.Element("type")));
            if (typeChildren.Count > 0 
                && language == Language.CSharp 
                && typeChildren[0] is TextRun text)
            {
                if (TryRemoveTypeRefPrefix(typeChildren, text, "readonly "))
                {
                    result.ReadWrite = ReadWrite.Read;
                }
                if (TryRemoveTypeRefPrefix(typeChildren, text, "override "))
                {
                    // Do nothing for now   
                }
            }

            var returnElements = (definition.Element("detaileddescription")
                ?.Elements("para")
                .Elements("simplesect")
                .Where(e => (string)e.Attribute("kind") == "return")
                .Select(e =>
                {
                    var returns = new Returns();
                    returns.Type.AddRange(typeChildren);
                    returns.Children.AddRange(ParseParagraphs(e));
                    return returns;
                })
                ?? Enumerable.Empty<Returns>()).ToList();
            if (returnElements.Count < 1)
            {
                var returns = new Returns();
                returns.Type.AddRange(typeChildren);
                returnElements.Add(returns);
            }
            result.Documentation.AddRange(returnElements);

            var parameterDescriptions = (definition.Element("detaileddescription")
                ?.Elements("para")
                .Elements("parameterlist")
                .Where(e => (string)e.Attribute("kind") == "param")
                .Elements("parameteritem")
                ?? Enumerable.Empty<XElement>())
                .ToDictionary(p => (string)p.Element("parameternamelist").Element("parametername")
                    , p => p.Element("parameterdescription"));
            result.Parameters.AddRange(definition.Elements("param")
                .Select(e =>
                {
                    var parameter = new Parameter()
                    {
                        DefaultValue = (string)e.Element("defval"),
                        Name = (string)e.Element("declname"),
                    };
                    parameter.Type.AddRange(ParseFormattingChildren(e.Element("type")));
                    if (parameterDescriptions.TryGetValue(parameter.Name, out var description))
                        parameter.Documentation.AddRange(ParseParagraphs(description));
                    return parameter;
                }));

            return result;
        }

        private static bool TryRemoveTypeRefPrefix(TypeReference typeReference, TextRun start, string prefix)
        {
            if (!start.Text.StartsWith(prefix))
                return false;

            if (start.Text.Length > prefix.Length)
                typeReference[0] = new TextRun(start.Text.Substring(prefix.Length));
            else
                typeReference.RemoveAt(0);
            return true;
        }

        private static SourceLocation GetLocation(XElement definition)
        {
            var location = definition.Element("location");
            if (location == null)
                return null;
            return new SourceLocation()
            {
                Path = (string)location.Attribute("file"),
                Line = (int)location.Attribute("line"),
                Column = (int)location.Attribute("column")
            };
        }

        private static IEnumerable<IElement> ParseParagraphs(XElement parent)
        {
            if (parent == null)
                return Enumerable.Empty<IElement>();
            return parent.Elements("para")
                .Select(p => new Paragraph(ParseFormattingChildren(p)));
        }

        private static IEnumerable<IElement> ParseFormattingChildren(XElement parent)
        {
            if (parent == null)
                yield break;

            foreach (var node in parent.Nodes())
            {
                if (node is XText text)
                    yield return new TextRun(text.Value);
                else if (node is XCData cData)
                    yield return new TextRun(cData.Value);
                else if (node is XElement element)
                {
                    switch (element.Name.LocalName)
                    {
                        case "ref":
                            yield return new Reference()
                            {
                                Id = (string)element.Attribute("refid"),
                                Kind = (string)element.Attribute("kindref"),
                                Text = (string)element
                            };
                            break;
                        case "a":
                            yield return new Hyperlink((string)element.Attribute("href"), ParseFormattingChildren(element));
                            break;
                        case "c":
                        case "computeroutput":
                            yield return new InlineCode(ParseFormattingChildren(element));
                            break;
                        case "br":
                        case "linebreak":
                            yield return new LineBreak();
                            break;
                        case "table":
                            var header = element.Elements("row").FirstOrDefault(r => r.Elements("entry").Any(c => (string)c.Attribute("thead") == "yes"));
                            var table = new Table()
                            {
                                DefinitionList = (int?)element.Attribute("cols") == 2,
                                Header = GetRow(header)
                            };
                            table.Rows.AddRange(element.Elements("row").Where(r => r != header).Select(GetRow));
                            yield return table;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }
        private static Row GetRow(XElement element)
        {
            if (element == null)
                return null;
            return new Row(element.Elements("entry").Select(e => ParseParagraphs(e).ToList()));
        }

        private static AccessModifier GetAccess(string access)
        {
            if (Enum.TryParse<AccessModifier>(access?.Replace(" ", ""), true, out var result))
                return result;
            if (access == "package")
                return AccessModifier.Internal;
            throw new InvalidCastException($"{access} cannot be converted to an access modifier");
        }
    }
}
