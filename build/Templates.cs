using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Fluid;
using Fluid.Ast;
using MicroElements.Metadata;
using Nuke.Common.ProjectModel;
using Parlot.Fluent;

namespace MicroElements.Build
{
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

        public class MicroElementsParser : FluidParser
        {
            public record TemplateData
            {
                public string TemplateName { get; init; }
                public string Arg1Name { get; init; }
                public string Arg2Name { get; init; }
                public IReadOnlyList<Statement> Statements { get; init; }
            }

            /// <inheritdoc />
            public MicroElementsParser()
            {
                Register2ArgsTag("xml_documentation", "elementName", "id", static async (tuple, writer, encoder, context) =>
                {
                    var elementName = tuple.arg1;
                    var id = tuple.arg2;

                    var xmlDocumentation = context.GetAmbientValue<XDocument>("xmlDocumentation");

                    var summaryById = GetElementById(xmlDocumentation, elementName, id);
                    if (summaryById is not null)
                        await writer.WriteAsync(summaryById);

                    return Completion.Normal;
                });

                RegisterParserTag("project_property2", Primary.And(ArgumentsList), ProjectPropertiesTag.WriteToAsync);

                RegisterParserBlock("template", Identifier.AndSkip(Comma).And(Identifier).AndSkip(Comma).And(Identifier),
                    async static (values, statements, writer, encoder, context) =>
                    {
                        var templateName = values.Item1;
                        var arg1Name = values.Item2;
                        var arg2Name = values.Item3;

                        context.AmbientValues.Add($"template_{templateName}", new TemplateData
                        {
                            TemplateName = templateName,
                            Arg1Name = arg1Name,
                            Arg2Name = arg2Name,
                            Statements = statements
                        });

                        return Completion.Normal;
                    });

                RegisterParserTag("render_template", Identifier.AndSkip(Comma).And(Primary).AndSkip(Comma).And(Identifier),
                    async static (values, writer, encoder, context) =>
                    {
                        var templateName = values.Item1;
                        var arg1Name = await values.Item2.EvaluateAsync(context);
                        var arg2Name = values.Item3;

                        if (context.GetAmbientValue<TemplateData>($"template_{templateName}") is {} template)
                        {
                            context.SetValue(template.Arg1Name, arg1Name.ToStringValue());
                            context.SetValue(template.Arg2Name, context.GetValue(arg2Name));

                            await template.Statements.RenderStatementsAsync(writer, encoder, context);
                        }

                        return Completion.Normal;
                    });

                
            }

            public void Register2ArgsTag(
                string tagName, string arg1Name = "arg1", string arg2Name = "arg2",
                Func<(string arg1, string arg2), TextWriter, TextEncoder, TemplateContext, ValueTask<Completion>> render = null)
            {
                string expected = $"{{% {tagName} {arg1Name}, {arg2Name}%}}";
                var parser = Identifier.ElseError($"Identifier '{arg1Name}' was expected after '{tagName}' tag. Full template: '{expected}'")
                    .AndSkip(Comma)
                    .And(Identifier.ElseError($"Identifier '{arg2Name}' was expected after first param and comma. Full template: '{expected}'"));

                RegisterParserTag(tagName, parser, render);
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

            parser.Register2ArgsTag("md_link", "projects_root", "project_name", (identifier, writer, encoder, context) =>
            {
                var projects_root = context.GetValue(identifier.arg1)?.ToStringValue();
                var project_name = context.GetValue(identifier.arg2)?.ToStringValue();
                string link = $"[{project_name}]({projects_root}/{project_name}/README.md)";
                writer.Write(link);

                return new ValueTask<Completion>(Completion.Normal);
            });

            return parser;
        }

        static TemplateContext CreateSolutionTemplateContext(BuildContext buildContext)
        {
            var context = new TemplateContext();

            context.AddAmbientValue(buildContext);

            return context;
        }

        static TemplateContext CreateProjectTemplateContext(BuildContext buildContext, ProjectConventions projectConventions)
        {
            var context = new TemplateContext();

            context.AddAmbientValue(buildContext);

            if (projectConventions.GetMetadata<IProjectXmlDocumentation>() is { } xmlDocumentation)
            {
                context.AddAmbientValue(xmlDocumentation.Document, "xmlDocumentation");
            }

            if (projectConventions.GetMetadata<IProjectProperties>() is { } projectProperties)
            {
                context.AddAmbientValue(projectProperties.Properties, "projectProperties");
            }

            return context;
        }

        public static ProjectConventions TryReadXmlDocumentation(this ProjectConventions conventions)
        {
            if (conventions.GetMetadata<IProjectXmlDocumentation>() is null)
            {
                var xmlDocumentationFile = conventions.XmlDocumentationFile;
                if (xmlDocumentationFile.FileExists())
                {
                    var xmlDocumentationContent = File.ReadAllText(xmlDocumentationFile);
                    XDocument xmlDocumentation = XDocument.Parse(xmlDocumentationContent);

                    conventions.SetMetadata((IProjectXmlDocumentation)new ProjectXmlDocumentation(xmlDocumentation));
                }
            }

            return conventions;
        }

        public static ProjectConventions AddProjectProperties(this ProjectConventions conventions)
        {
            if (conventions.GetMetadata<IProjectProperties>() is null)
            {
                var projectProperties = conventions.Project
                    .GetMSBuildProject()
                    .Properties
                    .ToDictionary(property => property.Name, property => property.EvaluatedValue);

                conventions.SetMetadata((IProjectProperties)new ProjectProperties(projectProperties));
            }

            return conventions;
        }

        public static bool TryRenderProjectReadme(BuildContext buildContext, ProjectConventions conventions)
        {
            var isReadmeChanged = false;

            var readmeFile = conventions.ReadmeFile;
            var readmeFileTemplate = conventions.ReadmeTemplateFile;

            conventions = conventions
                .TryReadXmlDocumentation()
                .AddProjectProperties();

            if (readmeFileTemplate.FileExists() && readmeFile.FileExists())
            {
                var templateContent = File.ReadAllText(readmeFileTemplate);

                var tryParse = Parser.TryParse(templateContent, out var template, out string error);
                if (tryParse)
                {
                    var context = CreateProjectTemplateContext(buildContext, conventions);

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

        public static bool TryRenderSharedReadme(BuildContext buildContext)
        {
            var isReadmeChanged = false;

            var solutionConventions = buildContext.SolutionConventions;

            var readmeFile = solutionConventions.ReadmeFile;
            var readmeFileTemplate = solutionConventions.ReadmeTemplateFile;

            if (readmeFileTemplate.FileExists() && readmeFile.FileExists())
            {
                var templateContent = File.ReadAllText(readmeFileTemplate);

                var tryParse = Parser.TryParse(templateContent, out var template, out string error);
                if (tryParse)
                {
                    var context = CreateSolutionTemplateContext(buildContext);

                    var rendered = template.Render(context);

                    var currentReadme = File.ReadAllText(readmeFile);

                    isReadmeChanged = rendered != currentReadme;
                    if (isReadmeChanged)
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

    public class ProjectPropertiesTag
    {
        public static async ValueTask<Completion> WriteToAsync(ValueTuple<Expression, List<FilterArgument>> tuple, TextWriter writer, TextEncoder encoder, TemplateContext context)
        {
            var arguments = new NamedExpressionList(tuple.Item2);

            var property = (await tuple.Item1.EvaluateAsync(context)).ToStringValue();
            //var propertyExpression = arguments["property", 0] ?? throw new ArgumentException("project_property tag requires a property argument");
            //var property = (await propertyExpression.EvaluateAsync(context)).ToStringValue();

            var projectExpression = arguments["project", 0] ?? throw new ArgumentException("project_property tag requires a project argument");
            var project = (await projectExpression.EvaluateAsync(context)).ToStringValue();

            var buildContext = context.GetAmbientValue<BuildContext>();
            if (buildContext != null)
            {
                var projectConventions = buildContext.ProjectConventions.FirstOrDefault(conventions => conventions.Project.Name == project);
                if (projectConventions?.GetMetadata<IProjectProperties>() is { } projectProperties)
                {
                    if (projectProperties.Properties.TryGetValue(property, out var value))
                    {
                        await writer.WriteAsync(value);
                    }
                }
            }

            return Completion.Normal;
        }
    }
}
