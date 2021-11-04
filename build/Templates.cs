using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using Fluid;
using Fluid.Ast;
using Nuke.Common.ProjectModel;
using Parlot.Fluent;

public static class Templates
{
    public static readonly FluidParser Parser = CreateParser();

    public static TemplateContext AddAmbientValue<T>(this TemplateContext context, T value, string? name = null)
    {
        name ??= typeof(T).FullName!;
        context.AmbientValues.Add(name, value);
        return context;
    }

    public static T? GetAmbientValue<T>(this TemplateContext context, string? name = null)
    {
        name ??= typeof(T).FullName!;
        return context.AmbientValues.TryGetValue(name, out var value)? (T)value : default;
    }

    class MicroElementsParser : FluidParser
    {
        /// <inheritdoc />
        public MicroElementsParser()
        {
            RegisterParserTag("xml_documentation", Identifier.AndSkip(Comma).And(Identifier),
                static async (tuple, writer, encoder, context) =>
                {
                    var elementName = tuple.Item1.ToString();
                    var id = tuple.Item2.ToString();

                    var xmlDocumentation = context.GetAmbientValue<XDocument>("xmlDocumentation");

                    var summaryById = GetElementById(xmlDocumentation, elementName, id);
                    if (summaryById is not null)
                        await writer.WriteAsync(summaryById);

                    return Completion.Normal;
                });
        }
    }

    static FluidParser CreateParser()
    {
        var parser = new MicroElementsParser();

        parser.RegisterIdentifierTag("readme", static async (identifier, writer, encoder, context) =>
        {
            var xmlDocumentation = context.GetAmbientValue<XDocument>("xmlDocumentation");

            var summaryById = GetElementById(xmlDocumentation, "readme", identifier);
            if(summaryById is not null)
                await writer.WriteAsync(summaryById);

            return Completion.Normal;
        });

        parser.RegisterIdentifierTag("summary", static async (identifier, writer, encoder, context) =>
        {
            var xmlDocumentation = context.GetAmbientValue<XDocument>("xmlDocumentation");

            var summaryById = GetElementById(xmlDocumentation, "summary", identifier);
            if (summaryById is not null)
                await writer.WriteAsync(summaryById);

            return Completion.Normal;
        });

        parser.RegisterIdentifierTag("project_property", static async (identifier, writer, encoder, context) =>
        {
            if (context.GetAmbientValue<Dictionary<string, string>>("projectProperties") is { } projectProperties)
            {
                if (projectProperties.TryGetValue(identifier, out var value))
                {
                    await writer.WriteAsync(value);
                }
            }

            return Completion.Normal;
        });

        return parser;
    }

    class xml_documentation
    {
        public int aa { get; set; }
    }

    static TemplateContext? CreateTemplateContext(
        ProjectConventions projectConventions, 
        XDocument? xmlDocumentation)
    {
        var options = new TemplateOptions();
        var context = new TemplateContext(options);

        context.AddAmbientValue(xmlDocumentation, "xmlDocumentation");

        var projectProperties = projectConventions.Project
            .GetMSBuildProject()
            .Properties
            .ToDictionary(property => property.Name, property => property.EvaluatedValue);
        context.AddAmbientValue(projectProperties, "projectProperties");

        return context;
    }

    public static bool TryRenderReadme(ProjectConventions conventions)
    {
        var isReadmeChanged = false;

        var readmeFile = conventions.ReadmeFile;
        var readmeFileTemplate = conventions.ReadmeTemplateFile;
        var xmlDocumentationFile = conventions.XmlDocumentationFile;

        if (readmeFileTemplate.FileExists() && readmeFile.FileExists() && xmlDocumentationFile.FileExists())
        {
            var xmlDocumentationContent = File.ReadAllText(xmlDocumentationFile);
            var xmlDocumentation = XDocument.Parse(xmlDocumentationContent);

            var templateContent = File.ReadAllText(readmeFileTemplate);

            var tryParse = Parser.TryParse(templateContent, out var template, out string error);
            if (tryParse)
            {
                var context = CreateTemplateContext(conventions, xmlDocumentation);

                var rendered = template.Render(context);

                var currentReadme = File.ReadAllText(readmeFile);

                isReadmeChanged = rendered != currentReadme;
                if(isReadmeChanged)
                    File.WriteAllText(readmeFile, rendered);
            }
            else
            {
                throw new Exception($"Failed to parse liquid template from '{readmeFileTemplate}'. Error: {error}");
            }
        }

        return isReadmeChanged;
    }

    static string? GetElementById(XDocument? xElements, string elementName, string id)
    {
        var summary = xElements?.XPathSelectElement($"//{elementName}[@id='{id}']")?.Value;
        if (summary != null)
        {
            int firstNotEmpty = 0;
            int lastNotEmpty = 0;
            var lines = summary.Split('\n').ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0 || lines[i].All(char.IsWhiteSpace))
                {
                    firstNotEmpty = i + 1;
                    break;
                }
            }

            for (int i = lines.Length-1; i > 0; i--)
            {
                if (lines[i].Length == 0 || lines[i].All(char.IsWhiteSpace))
                {
                    lastNotEmpty = i - 1;
                    break;
                }
            }

            var first = lines[firstNotEmpty];
            var countSpaces = first.TakeWhile(char.IsWhiteSpace).Count();

            StringBuilder stringBuilder = new StringBuilder();
            for (int i = firstNotEmpty; i <= lastNotEmpty; i++)
            {
                var line = lines[i];
                var lineTrimmed = line.Length > countSpaces ? line.Substring(countSpaces) : line;
                stringBuilder.Append(lineTrimmed);
                if(i != lastNotEmpty)
                    stringBuilder.AppendLine();
            }

            string summaryContent = stringBuilder.ToString();
            return summaryContent;
        }

        return null;
    }
}