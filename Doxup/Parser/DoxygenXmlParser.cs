using Doxup.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Doxup.Parser
{
    /* TODO:
     * - Fix explicity interface imlementations
     * - <remarks>
     * - <seealso>
     * 
     * https://github.com/EWSoftware/SHFB/tree/ea37113a5aa5e6d998213d37f43cc4f49acac2a7/SHFB/Source/CodeColorizer
     * 
     * https://ewsoftware.github.io/MAMLGuide/html/303c996a-2911-4c08-b492-6496c82b3edb.htm
     * 
     * https://github.com/doxygen/doxygen/blob/master/templates/xml/compound.xsd
     */

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
                    case "page":
                        var page = new Page();
                        page.Documentation.Add(new Heading(ParseFormattingChildren(definition.Element("title")))
                        {
                            Id = (string)definition.Attribute("id"),
                            Level = 1
                        });
                        page.Documentation.AddRange(ParseParagraphs(definition.Element("detaileddescription")));
                        project.Pages.Add(page);
                        break;
                    case "namespace":
                        foreach (var enumDef in definition.Descendants("memberdef")
                            .Where(e => (string)e.Attribute("kind") == "enum"))
                        {
                            project.Definitions.Add(ParseEnum(definition, enumDef));
                        }
                        break;
                    default:
                        project.Definitions.AddRange(definition.Elements("sectiondef")
                            .Elements("memberdef")
                            .Where(e => (string)e.Attribute("kind") == "enum")
                            .Select(e => ParseEnum(definition, e)));
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
                    member.InitialValue.AddRange(ParseFormattingChildren(e.Element("initializer")));
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
            result.Members.AddRange(definition.Elements("sectiondef")
                .Elements("memberdef")
                .Where(e => (string)e.Attribute("kind") != "enum")
                .Select(e => ParseMember(result.Language, e)));

            if (definition.Element("inheritancegraph") != null)
            {
                var graph = definition.Element("inheritancegraph").Elements("node")
                    .ToDictionary(e => (string)e.Attribute("id"), e => new
                    {
                        Element = e,
                        Inherits = new Inherits()
                        {
                            Id = (string)e.Element("link")?.Attribute("refid"),
                            Text = (string)e.Element("label")
                        }
                    });
                foreach (var value in graph.Values)
                {
                    value.Inherits.Children.AddRange(value.Element
                        .Elements("childnode")
                        .Select(e => graph[(string)e.Attribute("refid")].Inherits));
                }
                result.Inherits.AddRange(graph["1"].Inherits.Children);
            }
            else
            {
                result.Inherits.AddRange(definition.Elements("basecompoundref")
                    .Select(e => new Inherits()
                    {
                        Id = (string)e.Attribute("refid"),
                        Text = (string)e
                    }));
            }

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
            result.InitialValue.AddRange(ParseFormattingChildren(definition.Element("initializer")));

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

            result.Documentation.AddRange((definition.Element("detaileddescription")
                ?.Elements("para")
                .Elements("parameterlist")
                .Where(e => (string)e.Attribute("kind") == "exception")
                .Elements("parameteritem")
                ?? Enumerable.Empty<XElement>())
                .Select(e =>
                {
                    var error = new Error();
                    error.Type.Add(new TextRun((string)e.Element("parameternamelist").Element("parametername")));
                    error.Children.AddRange(ParseParagraphs(e.Element("parameterdescription")));
                    return error;
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

        private static IEnumerable<IVisual> ParseParagraphs(XElement parent)
        {
            var contents = ParseParagraphs(parent, null, 1);
            if (contents.Count == 1 && contents[0] is Paragraph paragraph)
                return paragraph.Children;
            return contents;
        }

        private static List<IVisual> ParseParagraphs(XElement parent, string sectionId, int level)
        {
            var contents = new List<IVisual>();
            if (parent == null)
                return contents;
            foreach (var element in parent.Elements())
            {
                if (element.Name.LocalName == "title")
                {
                    var heading = new Heading()
                    {
                        Id = sectionId,
                        Level = level
                    };
                    heading.Children.AddRange(ParseFormattingChildren(element));
                    contents.Add(heading);
                }
                else if (element.Name.LocalName == "para")
                {
                    var children = ParseFormattingChildren(element);
                    var start = 0;
                    for (var i = 0; i < children.Count; i++)
                    {
                        if (children[i] is IBlock)
                        {
                            if (start < i)
                                contents.Add(new Paragraph(children.Skip(start).Take(i - start)));
                            contents.Add(children[i]);
                            start = i + 1;
                        }
                    }
                    if (start < children.Count)
                        contents.Add(new Paragraph(children.Skip(start).Take(children.Count - start)));
                }
                else if (element.Name.LocalName == "sect1"
                    || element.Name.LocalName == "sect2"
                    || element.Name.LocalName == "sect3")
                {
                    contents.AddRange(ParseParagraphs(element, (string)element.Attribute("id"), level + 1));
                }
            }
            return contents;
        }

        private static List<IVisual> ParseFormattingChildren(XElement parent)
        {
            var result = new List<IVisual>();
            if (parent == null)
                return result;

            foreach (var node in parent.Nodes())
            {
                if (node is XText text)
                {
                    AppendText(result, text.Value);
                }
                else if (node is XCData cData)
                {
                    AppendText(result, cData.Value);
                }
                else if (node is XElement element)
                {
                    switch (element.Name.LocalName)
                    {
                        case "ref":
                            result.Add(new Reference()
                            {
                                Id = (string)element.Attribute("refid"),
                                Kind = (string)element.Attribute("kindref"),
                                Text = (string)element
                            });
                            break;
                        case "ulink":
                            result.Add(new Hyperlink((string)element.Attribute("url"), ParseFormattingChildren(element)));
                            break;
                        case "linebreak":
                            result.Add(new LineBreak());
                            break;
                        case "table":
                            var header = element.Elements("row").FirstOrDefault(r => r.Elements("entry").Any(c => (string)c.Attribute("thead") == "yes"));
                            var table = new Table()
                            {
                                DefinitionList = (int?)element.Attribute("cols") == 2,
                                Header = GetRow(header)
                            };
                            table.Rows.AddRange(element.Elements("row").Where(r => r != header).Select(GetRow));
                            result.Add(table);
                            break;
                        case "itemizedlist":
                            var unorderedList = new ListBlock(false);
                            unorderedList.Items.AddRange(element.Elements("listitem").Select(e => ParseParagraphs(e).ToList()));
                            result.Add(unorderedList);
                            break;
                        case "orderedlist":
                            var orderedList = new ListBlock(true);
                            orderedList.Items.AddRange(element.Elements("listitem").Select(e => ParseParagraphs(e).ToList()));
                            result.Add(orderedList);
                            break;
                        case "blockquote":
                            result.Add(new BlockQuote(ParseParagraphs(element)));
                            break;
                        case "verbatim":
                            result.Add(new CodeBlock(ParseFormattingChildren(element)));
                            break;
                        case "programlisting":
                            result.Add(new CodeBlock(element
                                .Elements("codeline")
                                .Select(e => new Paragraph(ParseFormattingChildren(e))))
                            {
                                Language = new Language((string)element.Attribute("filename"))
                            });
                            break;
                        case "highlight":
                            if (!Enum.TryParse((string)element.Attribute("class") ?? "normal", true, out HighlightStyle style))
                                throw new InvalidOperationException("Invalid highlight style");
                            result.Add(new Highlight(style, ParseFormattingChildren(element)));
                            break;
                        case "hruler":
                            result.Add(new HorizontalRule());
                            break;
                        case "image":
                            result.Add(new Image()
                            {
                                AltText = (string)element.Attribute("alt"),
                                Src = (string)element.Attribute("name")
                            });
                            break;
                        case "sp":
                            AppendText(result, " ");
                            break;
                        default:
                            if (StyledSpan.TryGetStyle(element.Name.LocalName, out var inlineStyle))
                                result.Add(new StyledSpan(inlineStyle, ParseFormattingChildren(element)));
                            else if (TryGetEntity(element.Name.LocalName, out var entity))
                                AppendText(result, new string(entity, 1));
                            else
                                throw new NotSupportedException();
                            break;
                    }
                }
            }
            return result;
        }

        private static void AppendText(List<IVisual> content, string text)
        {
            if (content.LastOrDefault() is TextRun textRun)
                textRun.Text += text;
            else
                content.Add(new TextRun(text));
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

        private static bool TryGetEntity(string name, out char value)
        {
            switch (name)
            {
                case "nbsp": value = '\u00A0'; return true; // no-break space = non-breaking space, U+00A0 ISOnum
                case "iexcl": value = '\u00A1'; return true; // inverted exclamation mark, U+00A1 ISOnum
                case "cent": value = '\u00A2'; return true; // cent sign, U+00A2 ISOnum
                case "pound": value = '\u00A3'; return true; // pound sign, U+00A3 ISOnum
                case "curren": value = '\u00A4'; return true; // currency sign, U+00A4 ISOnum
                case "yen": value = '\u00A5'; return true; // yen sign = yuan sign, U+00A5 ISOnum
                case "brvbar": value = '\u00A6'; return true; // broken bar = broken vertical bar, U+00A6 ISOnum
                case "sect": value = '\u00A7'; return true; // section sign, U+00A7 ISOnum
                case "uml": value = '\u00A8'; return true; // diaeresis = spacing diaeresis, U+00A8 ISOdia
                case "copy": value = '\u00A9'; return true; // copyright sign, U+00A9 ISOnum
                case "ordf": value = '\u00AA'; return true; // feminine ordinal indicator, U+00AA ISOnum
                case "laquo": value = '\u00AB'; return true; // left-pointing double angle quotation mark = left pointing guillemet, U+00AB ISOnum
                case "not": value = '\u00AC'; return true; // not sign, U+00AC ISOnum
                case "shy": value = '\u00AD'; return true; // soft hyphen = discretionary hyphen, U+00AD ISOnum
                case "reg": value = '\u00AE'; return true; // registered sign = registered trade mark sign, U+00AE ISOnum
                case "macr": value = '\u00AF'; return true; // macron = spacing macron = overline = APL overbar, U+00AF ISOdia
                case "deg": value = '\u00B0'; return true; // degree sign, U+00B0 ISOnum
                case "plusmn": value = '\u00B1'; return true; // plus-minus sign = plus-or-minus sign, U+00B1 ISOnum
                case "sup2": value = '\u00B2'; return true; // superscript two = superscript digit two = squared, U+00B2 ISOnum
                case "sup3": value = '\u00B3'; return true; // superscript three = superscript digit three = cubed, U+00B3 ISOnum
                case "acute": value = '\u00B4'; return true; // acute accent = spacing acute, U+00B4 ISOdia
                case "micro": value = '\u00B5'; return true; // micro sign, U+00B5 ISOnum
                case "para": value = '\u00B6'; return true; // pilcrow sign = paragraph sign, U+00B6 ISOnum
                case "middot": value = '\u00B7'; return true; // middle dot = Georgian comma = Greek middle dot, U+00B7 ISOnum
                case "cedil": value = '\u00B8'; return true; // cedilla = spacing cedilla, U+00B8 ISOdia
                case "sup1": value = '\u00B9'; return true; // superscript one = superscript digit one, U+00B9 ISOnum
                case "ordm": value = '\u00BA'; return true; // masculine ordinal indicator, U+00BA ISOnum
                case "raquo": value = '\u00BB'; return true; // right-pointing double angle quotation mark = right pointing guillemet, U+00BB ISOnum
                case "frac14": value = '\u00BC'; return true; // vulgar fraction one quarter = fraction one quarter, U+00BC ISOnum
                case "frac12": value = '\u00BD'; return true; // vulgar fraction one half = fraction one half, U+00BD ISOnum
                case "frac34": value = '\u00BE'; return true; // vulgar fraction three quarters = fraction three quarters, U+00BE ISOnum
                case "iquest": value = '\u00BF'; return true; // inverted question mark = turned question mark, U+00BF ISOnum
                case "Agrave": value = '\u00C0'; return true; // latin capital letter A with grave = latin capital letter A grave, U+00C0 ISOlat1
                case "Aacute": value = '\u00C1'; return true; // latin capital letter A with acute, U+00C1 ISOlat1
                case "Acirc": value = '\u00C2'; return true; // latin capital letter A with circumflex, U+00C2 ISOlat1
                case "Atilde": value = '\u00C3'; return true; // latin capital letter A with tilde, U+00C3 ISOlat1
                case "Auml": value = '\u00C4'; return true; // latin capital letter A with diaeresis, U+00C4 ISOlat1
                case "Aring": value = '\u00C5'; return true; // latin capital letter A with ring above = latin capital letter A ring, U+00C5 ISOlat1
                case "AElig": value = '\u00C6'; return true; // latin capital letter AE = latin capital ligature AE, U+00C6 ISOlat1
                case "Ccedil": value = '\u00C7'; return true; // latin capital letter C with cedilla, U+00C7 ISOlat1
                case "Egrave": value = '\u00C8'; return true; // latin capital letter E with grave, U+00C8 ISOlat1
                case "Eacute": value = '\u00C9'; return true; // latin capital letter E with acute, U+00C9 ISOlat1
                case "Ecirc": value = '\u00CA'; return true; // latin capital letter E with circumflex, U+00CA ISOlat1
                case "Euml": value = '\u00CB'; return true; // latin capital letter E with diaeresis, U+00CB ISOlat1
                case "Igrave": value = '\u00CC'; return true; // latin capital letter I with grave, U+00CC ISOlat1
                case "Iacute": value = '\u00CD'; return true; // latin capital letter I with acute, U+00CD ISOlat1
                case "Icirc": value = '\u00CE'; return true; // latin capital letter I with circumflex, U+00CE ISOlat1
                case "Iuml": value = '\u00CF'; return true; // latin capital letter I with diaeresis, U+00CF ISOlat1
                case "ETH": value = '\u00D0'; return true; // latin capital letter ETH, U+00D0 ISOlat1
                case "Ntilde": value = '\u00D1'; return true; // latin capital letter N with tilde, U+00D1 ISOlat1
                case "Ograve": value = '\u00D2'; return true; // latin capital letter O with grave, U+00D2 ISOlat1
                case "Oacute": value = '\u00D3'; return true; // latin capital letter O with acute, U+00D3 ISOlat1
                case "Ocirc": value = '\u00D4'; return true; // latin capital letter O with circumflex, U+00D4 ISOlat1
                case "Otilde": value = '\u00D5'; return true; // latin capital letter O with tilde, U+00D5 ISOlat1
                case "Ouml": value = '\u00D6'; return true; // latin capital letter O with diaeresis, U+00D6 ISOlat1
                case "times": value = '\u00D7'; return true; // multiplication sign, U+00D7 ISOnum
                case "Oslash": value = '\u00D8'; return true; // latin capital letter O with stroke = latin capital letter O slash, U+00D8 ISOlat1
                case "Ugrave": value = '\u00D9'; return true; // latin capital letter U with grave, U+00D9 ISOlat1
                case "Uacute": value = '\u00DA'; return true; // latin capital letter U with acute, U+00DA ISOlat1
                case "Ucirc": value = '\u00DB'; return true; // latin capital letter U with circumflex, U+00DB ISOlat1
                case "Uuml": value = '\u00DC'; return true; // latin capital letter U with diaeresis, U+00DC ISOlat1
                case "Yacute": value = '\u00DD'; return true; // latin capital letter Y with acute, U+00DD ISOlat1
                case "THORN": value = '\u00DE'; return true; // latin capital letter THORN, U+00DE ISOlat1
                case "szlig": value = '\u00DF'; return true; // latin small letter sharp s = ess-zed, U+00DF ISOlat1
                case "agrave": value = '\u00E0'; return true; // latin small letter a with grave = latin small letter a grave, U+00E0 ISOlat1
                case "aacute": value = '\u00E1'; return true; // latin small letter a with acute, U+00E1 ISOlat1
                case "acirc": value = '\u00E2'; return true; // latin small letter a with circumflex, U+00E2 ISOlat1
                case "atilde": value = '\u00E3'; return true; // latin small letter a with tilde, U+00E3 ISOlat1
                case "auml": value = '\u00E4'; return true; // latin small letter a with diaeresis, U+00E4 ISOlat1
                case "aring": value = '\u00E5'; return true; // latin small letter a with ring above = latin small letter a ring, U+00E5 ISOlat1
                case "aelig": value = '\u00E6'; return true; // latin small letter ae = latin small ligature ae, U+00E6 ISOlat1
                case "ccedil": value = '\u00E7'; return true; // latin small letter c with cedilla, U+00E7 ISOlat1
                case "egrave": value = '\u00E8'; return true; // latin small letter e with grave, U+00E8 ISOlat1
                case "eacute": value = '\u00E9'; return true; // latin small letter e with acute, U+00E9 ISOlat1
                case "ecirc": value = '\u00EA'; return true; // latin small letter e with circumflex, U+00EA ISOlat1
                case "euml": value = '\u00EB'; return true; // latin small letter e with diaeresis, U+00EB ISOlat1
                case "igrave": value = '\u00EC'; return true; // latin small letter i with grave, U+00EC ISOlat1
                case "iacute": value = '\u00ED'; return true; // latin small letter i with acute, U+00ED ISOlat1
                case "icirc": value = '\u00EE'; return true; // latin small letter i with circumflex, U+00EE ISOlat1
                case "iuml": value = '\u00EF'; return true; // latin small letter i with diaeresis, U+00EF ISOlat1
                case "eth": value = '\u00F0'; return true; // latin small letter eth, U+00F0 ISOlat1
                case "ntilde": value = '\u00F1'; return true; // latin small letter n with tilde, U+00F1 ISOlat1
                case "ograve": value = '\u00F2'; return true; // latin small letter o with grave, U+00F2 ISOlat1
                case "oacute": value = '\u00F3'; return true; // latin small letter o with acute, U+00F3 ISOlat1
                case "ocirc": value = '\u00F4'; return true; // latin small letter o with circumflex, U+00F4 ISOlat1
                case "otilde": value = '\u00F5'; return true; // latin small letter o with tilde, U+00F5 ISOlat1
                case "ouml": value = '\u00F6'; return true; // latin small letter o with diaeresis, U+00F6 ISOlat1
                case "divide": value = '\u00F7'; return true; // division sign, U+00F7 ISOnum
                case "oslash": value = '\u00F8'; return true; // latin small letter o with stroke, = latin small letter o slash, U+00F8 ISOlat1
                case "ugrave": value = '\u00F9'; return true; // latin small letter u with grave, U+00F9 ISOlat1
                case "uacute": value = '\u00FA'; return true; // latin small letter u with acute, U+00FA ISOlat1
                case "ucirc": value = '\u00FB'; return true; // latin small letter u with circumflex, U+00FB ISOlat1
                case "uuml": value = '\u00FC'; return true; // latin small letter u with diaeresis, U+00FC ISOlat1
                case "yacute": value = '\u00FD'; return true; // latin small letter y with acute, U+00FD ISOlat1
                case "thorn": value = '\u00FE'; return true; // latin small letter thorn, U+00FE ISOlat1
                case "yuml": value = '\u00FF'; return true; // latin small letter y with diaeresis, U+00FF ISOlat1
                case "fnof": value = '\u0192'; return true; // latin small f with hook = function = florin, U+0192 ISOtech
                case "Alpha": value = '\u0391'; return true; // greek capital letter alpha, U+0391
                case "Beta": value = '\u0392'; return true; // greek capital letter beta, U+0392
                case "Gamma": value = '\u0393'; return true; // greek capital letter gamma, U+0393 ISOgrk3
                case "Delta": value = '\u0394'; return true; // greek capital letter delta, U+0394 ISOgrk3
                case "Epsilon": value = '\u0395'; return true; // greek capital letter epsilon, U+0395
                case "Zeta": value = '\u0396'; return true; // greek capital letter zeta, U+0396
                case "Eta": value = '\u0397'; return true; // greek capital letter eta, U+0397
                case "Theta": value = '\u0398'; return true; // greek capital letter theta, U+0398 ISOgrk3
                case "Iota": value = '\u0399'; return true; // greek capital letter iota, U+0399
                case "Kappa": value = '\u039A'; return true; // greek capital letter kappa, U+039A
                case "Lambda": value = '\u039B'; return true; // greek capital letter lambda, U+039B ISOgrk3
                case "Mu": value = '\u039C'; return true; // greek capital letter mu, U+039C
                case "Nu": value = '\u039D'; return true; // greek capital letter nu, U+039D
                case "Xi": value = '\u039E'; return true; // greek capital letter xi, U+039E ISOgrk3
                case "Omicron": value = '\u039F'; return true; // greek capital letter omicron, U+039F
                case "Pi": value = '\u03A0'; return true; // greek capital letter pi, U+03A0 ISOgrk3
                case "Rho": value = '\u03A1'; return true; // greek capital letter rho, U+03A1
                case "Sigma": value = '\u03A3'; return true; // greek capital letter sigma, U+03A3 ISOgrk3
                case "Tau": value = '\u03A4'; return true; // greek capital letter tau, U+03A4
                case "Upsilon": value = '\u03A5'; return true; // greek capital letter upsilon, U+03A5 ISOgrk3
                case "Phi": value = '\u03A6'; return true; // greek capital letter phi, U+03A6 ISOgrk3
                case "Chi": value = '\u03A7'; return true; // greek capital letter chi, U+03A7
                case "Psi": value = '\u03A8'; return true; // greek capital letter psi, U+03A8 ISOgrk3
                case "Omega": value = '\u03A9'; return true; // greek capital letter omega, U+03A9 ISOgrk3
                case "alpha": value = '\u03B1'; return true; // greek small letter alpha, U+03B1 ISOgrk3
                case "beta": value = '\u03B2'; return true; // greek small letter beta, U+03B2 ISOgrk3
                case "gamma": value = '\u03B3'; return true; // greek small letter gamma, U+03B3 ISOgrk3
                case "delta": value = '\u03B4'; return true; // greek small letter delta, U+03B4 ISOgrk3
                case "epsilon": value = '\u03B5'; return true; // greek small letter epsilon, U+03B5 ISOgrk3
                case "zeta": value = '\u03B6'; return true; // greek small letter zeta, U+03B6 ISOgrk3
                case "eta": value = '\u03B7'; return true; // greek small letter eta, U+03B7 ISOgrk3
                case "theta": value = '\u03B8'; return true; // greek small letter theta, U+03B8 ISOgrk3
                case "iota": value = '\u03B9'; return true; // greek small letter iota, U+03B9 ISOgrk3
                case "kappa": value = '\u03BA'; return true; // greek small letter kappa, U+03BA ISOgrk3
                case "lambda": value = '\u03BB'; return true; // greek small letter lambda, U+03BB ISOgrk3
                case "mu": value = '\u03BC'; return true; // greek small letter mu, U+03BC ISOgrk3
                case "nu": value = '\u03BD'; return true; // greek small letter nu, U+03BD ISOgrk3
                case "xi": value = '\u03BE'; return true; // greek small letter xi, U+03BE ISOgrk3
                case "omicron": value = '\u03BF'; return true; // greek small letter omicron, U+03BF NEW
                case "pi": value = '\u03C0'; return true; // greek small letter pi, U+03C0 ISOgrk3
                case "rho": value = '\u03C1'; return true; // greek small letter rho, U+03C1 ISOgrk3
                case "sigmaf": value = '\u03C2'; return true; // greek small letter final sigma, U+03C2 ISOgrk3
                case "sigma": value = '\u03C3'; return true; // greek small letter sigma, U+03C3 ISOgrk3
                case "tau": value = '\u03C4'; return true; // greek small letter tau, U+03C4 ISOgrk3
                case "upsilon": value = '\u03C5'; return true; // greek small letter upsilon, U+03C5 ISOgrk3
                case "phi": value = '\u03C6'; return true; // greek small letter phi, U+03C6 ISOgrk3
                case "chi": value = '\u03C7'; return true; // greek small letter chi, U+03C7 ISOgrk3
                case "psi": value = '\u03C8'; return true; // greek small letter psi, U+03C8 ISOgrk3
                case "omega": value = '\u03C9'; return true; // greek small letter omega, U+03C9 ISOgrk3
                case "thetasym": value = '\u03D1'; return true; // greek small letter theta symbol, U+03D1 NEW
                case "upsih": value = '\u03D2'; return true; // greek upsilon with hook symbol, U+03D2 NEW
                case "piv": value = '\u03D6'; return true; // greek pi symbol, U+03D6 ISOgrk3
                case "bull": value = '\u2022'; return true; // bullet = black small circle, U+2022 ISOpub
                case "hellip": value = '\u2026'; return true; // horizontal ellipsis = three dot leader, U+2026 ISOpub
                case "prime": value = '\u2032'; return true; // prime = minutes = feet, U+2032 ISOtech
                case "Prime": value = '\u2033'; return true; // double prime = seconds = inches, U+2033 ISOtech
                case "oline": value = '\u203E'; return true; // overline = spacing overscore, U+203E NEW
                case "frasl": value = '\u2044'; return true; // fraction slash, U+2044 NEW
                case "weierp": value = '\u2118'; return true; // script capital P = power set = Weierstrass p, U+2118 ISOamso
                case "image": value = '\u2111'; return true; // blackletter capital I = imaginary part, U+2111 ISOamso
                case "real": value = '\u211C'; return true; // blackletter capital R = real part symbol, U+211C ISOamso
                case "trade": value = '\u2122'; return true; // trade mark sign, U+2122 ISOnum
                case "alefsym": value = '\u2135'; return true; // alef symbol = first transfinite cardinal, U+2135 NEW
                case "larr": value = '\u2190'; return true; // leftwards arrow, U+2190 ISOnum
                case "uarr": value = '\u2191'; return true; // upwards arrow, U+2191 ISOnum-->
                case "rarr": value = '\u2192'; return true; // rightwards arrow, U+2192 ISOnum
                case "darr": value = '\u2193'; return true; // downwards arrow, U+2193 ISOnum
                case "harr": value = '\u2194'; return true; // left right arrow, U+2194 ISOamsa
                case "crarr": value = '\u21B5'; return true; // downwards arrow with corner leftwards = carriage return, U+21B5 NEW
                case "lArr": value = '\u21D0'; return true; // leftwards double arrow, U+21D0 ISOtech
                case "uArr": value = '\u21D1'; return true; // upwards double arrow, U+21D1 ISOamsa
                case "rArr": value = '\u21D2'; return true; // rightwards double arrow, U+21D2 ISOtech
                case "dArr": value = '\u21D3'; return true; // downwards double arrow, U+21D3 ISOamsa
                case "hArr": value = '\u21D4'; return true; // left right double arrow, U+21D4 ISOamsa
                case "forall": value = '\u2200'; return true; // for all, U+2200 ISOtech
                case "part": value = '\u2202'; return true; // partial differential, U+2202 ISOtech
                case "exist": value = '\u2203'; return true; // there exists, U+2203 ISOtech
                case "empty": value = '\u2205'; return true; // empty set = null set = diameter, U+2205 ISOamso
                case "nabla": value = '\u2207'; return true; // nabla = backward difference, U+2207 ISOtech
                case "isin": value = '\u2208'; return true; // element of, U+2208 ISOtech
                case "notin": value = '\u2209'; return true; // not an element of, U+2209 ISOtech
                case "ni": value = '\u220B'; return true; // contains as member, U+220B ISOtech
                case "prod": value = '\u220F'; return true; // n-ary product = product sign, U+220F ISOamsb
                case "sum": value = '\u2211'; return true; // n-ary sumation, U+2211 ISOamsb
                case "minus": value = '\u2212'; return true; // minus sign, U+2212 ISOtech
                case "lowast": value = '\u2217'; return true; // asterisk operator, U+2217 ISOtech
                case "radic": value = '\u221A'; return true; // square root = radical sign, U+221A ISOtech
                case "prop": value = '\u221D'; return true; // proportional to, U+221D ISOtech
                case "infin": value = '\u221E'; return true; // infinity, U+221E ISOtech
                case "ang": value = '\u2220'; return true; // angle, U+2220 ISOamso
                case "and": value = '\u2227'; return true; // logical and = wedge, U+2227 ISOtech
                case "or": value = '\u2228'; return true; // logical or = vee, U+2228 ISOtech
                case "cap": value = '\u2229'; return true; // intersection = cap, U+2229 ISOtech
                case "cup": value = '\u222A'; return true; // union = cup, U+222A ISOtech
                case "int": value = '\u222B'; return true; // integral, U+222B ISOtech
                case "there4": value = '\u2234'; return true; // therefore, U+2234 ISOtech
                case "sim": value = '\u223C'; return true; // tilde operator = varies with = similar to, U+223C ISOtech
                case "cong": value = '\u2245'; return true; // approximately equal to, U+2245 ISOtech
                case "asymp": value = '\u2248'; return true; // almost equal to = asymptotic to, U+2248 ISOamsr
                case "ne": value = '\u2260'; return true; // not equal to, U+2260 ISOtech
                case "equiv": value = '\u2261'; return true; // identical to, U+2261 ISOtech
                case "le": value = '\u2264'; return true; // less-than or equal to, U+2264 ISOtech
                case "ge": value = '\u2265'; return true; // greater-than or equal to, U+2265 ISOtech
                case "sub": value = '\u2282'; return true; // subset of, U+2282 ISOtech
                case "sup": value = '\u2283'; return true; // superset of, U+2283 ISOtech
                case "nsub": value = '\u2284'; return true; // not a subset of, U+2284 ISOamsn
                case "sube": value = '\u2286'; return true; // subset of or equal to, U+2286 ISOtech
                case "supe": value = '\u2287'; return true; // superset of or equal to, U+2287 ISOtech
                case "oplus": value = '\u2295'; return true; // circled plus = direct sum, U+2295 ISOamsb
                case "otimes": value = '\u2297'; return true; // circled times = vector product, U+2297 ISOamsb
                case "perp": value = '\u22A5'; return true; // up tack = orthogonal to = perpendicular, U+22A5 ISOtech
                case "sdot": value = '\u22C5'; return true; // dot operator, U+22C5 ISOamsb
                case "lceil": value = '\u2308'; return true; // left ceiling = apl upstile, U+2308 ISOamsc
                case "rceil": value = '\u2309'; return true; // right ceiling, U+2309 ISOamsc
                case "lfloor": value = '\u230A'; return true; // left floor = apl downstile, U+230A ISOamsc
                case "rfloor": value = '\u230B'; return true; // right floor, U+230B ISOamsc
                case "lang": value = '\u2329'; return true; // left-pointing angle bracket = bra, U+2329 ISOtech
                case "rang": value = '\u232A'; return true; // right-pointing angle bracket = ket, U+232A ISOtech
                case "loz": value = '\u25CA'; return true; // lozenge, U+25CA ISOpub
                case "spades": value = '\u2660'; return true; // black spade suit, U+2660 ISOpub
                case "clubs": value = '\u2663'; return true; // black club suit = shamrock, U+2663 ISOpub
                case "hearts": value = '\u2665'; return true; // black heart suit = valentine, U+2665 ISOpub
                case "diams": value = '\u2666'; return true; // black diamond suit, U+2666 ISOpub
                case "quot": value = '\u0022'; return true; // quotation mark = APL quote, U+0022 ISOnum
                case "amp": value = '\u0026'; return true; // ampersand, U+0026 ISOnum
                case "lt": value = '\u003C'; return true; // less-than sign, U+003C ISOnum
                case "gt": value = '\u003E'; return true; // greater-than sign, U+003E ISOnum
                case "OElig": value = '\u0152'; return true; // latin capital ligature OE, U+0152 ISOlat2
                case "oelig": value = '\u0153'; return true; // latin small ligature oe, U+0153 ISOlat2
                case "Scaron": value = '\u0160'; return true; // latin capital letter S with caron, U+0160 ISOlat2
                case "scaron": value = '\u0161'; return true; // latin small letter s with caron, U+0161 ISOlat2
                case "Yuml": value = '\u0178'; return true; // latin capital letter Y with diaeresis, U+0178 ISOlat2
                case "circ": value = '\u02C6'; return true; // modifier letter circumflex accent, U+02C6 ISOpub
                case "tilde": value = '\u02DC'; return true; // small tilde, U+02DC ISOdia
                case "ensp": value = '\u2002'; return true; // en space, U+2002 ISOpub
                case "emsp": value = '\u2003'; return true; // em space, U+2003 ISOpub
                case "thinsp": value = '\u2009'; return true; // thin space, U+2009 ISOpub
                case "zwnj": value = '\u200C'; return true; // zero width non-joiner, U+200C NEW RFC 2070
                case "zwj": value = '\u200D'; return true; // zero width joiner, U+200D NEW RFC 2070
                case "lrm": value = '\u200E'; return true; // left-to-right mark, U+200E NEW RFC 2070
                case "rlm": value = '\u200F'; return true; // right-to-left mark, U+200F NEW RFC 2070
                case "ndash": value = '\u2013'; return true; // en dash, U+2013 ISOpub
                case "mdash": value = '\u2014'; return true; // em dash, U+2014 ISOpub
                case "lsquo": value = '\u2018'; return true; // left single quotation mark, U+2018 ISOnum
                case "rsquo": value = '\u2019'; return true; // right single quotation mark, U+2019 ISOnum
                case "sbquo": value = '\u201A'; return true; // single low-9 quotation mark, U+201A NEW
                case "ldquo": value = '\u201C'; return true; // left double quotation mark, U+201C ISOnum
                case "rdquo": value = '\u201D'; return true; // right double quotation mark, U+201D ISOnum
                case "bdquo": value = '\u201E'; return true; // double low-9 quotation mark, U+201E NEW
                case "dagger": value = '\u2020'; return true; // dagger, U+2020 ISOpub
                case "Dagger": value = '\u2021'; return true; // double dagger, U+2021 ISOpub
                case "permil": value = '\u2030'; return true; // per mille sign, U+2030 ISOtech
                case "lsaquo": value = '\u2039'; return true; // single left-pointing angle quotation mark, U+2039 ISO proposed
                case "rsaquo": value = '\u203A'; return true; // single right-pointing angle quotation mark, U+203A ISO proposed
                case "euro": value = '\u20AC'; return true; // euro sign, U+20AC NEW
            }
            value = '\0';
            return false;
        }
    }
}
