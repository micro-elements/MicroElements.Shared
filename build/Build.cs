using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using MicroElements.Metadata;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Logger;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using MemberExpression = System.Linq.Expressions.MemberExpression;

#region Supressions

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Nuke targets are private")]

#endregion

//*****************************************************************
// Support plugins are available for:
//  - JetBrains ReSharper        https://nuke.build/resharper
//   - JetBrains Rider            https://nuke.build/rider
//   - Microsoft VisualStudio     https://nuke.build/visualstudio
//   - Microsoft VSCode           https://nuke.build/vscode
//*****************************************************************

namespace MicroElements.Build
{
    partial class Build
    {
        public static int Main() => Execute<Build>(x => x.BuildFromVS);

        Target BuildFromVS => _ => _
            .DependsOn(BuildAndPack, Docs, Test)
            .Executes(() =>
            {
                // To properly reload projects
                Solution.Save();
            });

        Target BuildDocs => _ => _
            .DependsOn(Docs)
            .Executes();

        Target GitHubActions => _ => _
            .DependsOn(BuildAndPack, Test)
            .Executes();

        Target GitHubActionsPublish => _ => _
            .DependsOn(BuildAndPack, Test, Push)
            .Executes();
    }

    [CheckBuildProjectConfigurations]
    [ShutdownDotNetAfterServerBuild]
    partial class Build : NukeBuild
    {
        static readonly string[] ProjectsToBuild = new string[]
        {
            "MicroElements.IsExternalInit",
            "MicroElements.JetBrains.Sources",
            "MicroElements.CodeContracts.Sources",
            "MicroElements.Collections.Sources",
            "MicroElements.Formatting.Sources",
            "MicroElements.Text.Sources",
            "MicroElements.Reflection.Sources",
            //"MicroElements.Reflection",
            "MicroElements.Shared.Sources",
            "MicroElements.Logging",
        };

        static readonly string[] TestProjects = new string[]
        {
            "MicroElements.Shared.Tests",
            "MicroElements.Logging.Tests",
        };

        #region Build Arguments

        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

        [Parameter("Nuget Url for upload")]
        string UPLOAD_NUGET = "https://api.nuget.org/v3/index.json";

        [Parameter("Nuget ApiKey")]
        string? UPLOAD_NUGET_API_KEY;

        [Parameter("Projects to build. Project will be build if project name is in build pattern.")]
        string? BUILD_PATTERN;

        [Parameter("Projects to upload. Project will be uploaded if project name is in upload pattern.")]
        string? UPLOAD_PATTERN;

        [Parameter("PublishTestResults")]
        bool PublishTestResults = true;

        #endregion

        [Solution] readonly Solution Solution = null!;
        [GitRepository] readonly GitRepository GitRepository = null!;
        BuildContext Context = null!;

        /// <inheritdoc />
        protected override void OnBuildInitialized()
        {
            base.OnBuildInitialized();

            Context = new()
            {
                Context = new(StringComparer.OrdinalIgnoreCase),
                SolutionConventions = new SolutionConventions(Solution)
            };
        }

        public AbsolutePath SolutionDirectory => RootDirectory;

        public AbsolutePath SourceDirectory => SolutionDirectory / "src";

        public AbsolutePath TestsDirectory => SolutionDirectory / "tests";

        public AbsolutePath ArtifactsDirectory => SolutionDirectory / "artifacts";

        public AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";

        Target DumpArguments => _ => _
            .Before(Clean)
            .Executes(() =>
            {
                DumpArg(build => IsLocalBuild);

                DumpArg(build => build.Configuration);
                DumpArg(build => build.UPLOAD_NUGET);
                DumpArg(build => build.UPLOAD_NUGET_API_KEY, isSecured:true);
                DumpArg(build => build.BUILD_PATTERN);
                DumpArg(build => build.UPLOAD_PATTERN);

                DumpArg("RepositoryUrl", GitRepository.HttpsUrl);
                DumpArg("PackageProjectUrl", $"https://github.com/{GitRepository.Identifier}");
            });

        public void DumpArg<TProperty>(
            Expression<Func<Build, TProperty>> expression,
            bool isSecured = false,
            string nullPlaceholder = "null")
        {
            var memberExpression = (MemberExpression)expression.Body;

            string propertyName = "";
            object? propertyValue = null;

            if (memberExpression.Member is FieldInfo fieldInfo)
            {
                propertyName = fieldInfo.Name;
                propertyValue = fieldInfo.GetValue(this);
            }

            if (memberExpression.Member is PropertyInfo propertyInfo)
            {
                propertyName = propertyInfo.Name;
                propertyValue = propertyInfo.GetValue(this);
            }

            DumpArg(propertyName, propertyValue, isSecured, nullPlaceholder);
        }

        public void DumpArg(
            string propertyName,
            object? propertyValue,
            bool isSecured = false,
            string nullPlaceholder = "null")
        {
            string textValue = (propertyValue != null ? propertyValue.ToString() : nullPlaceholder) ?? nullPlaceholder;
            if (isSecured && propertyValue != null)
                textValue = "***SECURED***";

            Info($"{propertyName}: {textValue}");
        }


        Target Clean => _ => _
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
                TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
                EnsureCleanDirectory(ArtifactsDirectory);
            });

        Target BuildAndPack => _ => _
            .Produces(ArtifactsDirectory / "*.nupkg")
            .DependsOn(DumpArguments, Clean)
            .Executes(() =>
            {
                var projectConventionsList = new List<ProjectConventions>();

                foreach (var projectToBuild in ProjectsToBuild)
                {
                    bool shouldBuild = projectToBuild.IsMatchesPattern(BUILD_PATTERN);
                    if (!shouldBuild)
                    {
                        Info($"Project '{projectToBuild}' is not matches {nameof(BUILD_PATTERN)} '{BUILD_PATTERN}'. Build skipped.");
                        continue;
                    }

                    var project = Solution.GetProject(projectToBuild);
                    if (project is null)
                    {
                        Error($"Project '{projectToBuild}' is not found.");
                        continue;
                    }

                    var projectConventions = new ProjectConventions(project, Configuration);
                    projectConventionsList.Add(projectConventions);

                    var changelogFile = project.Directory / "CHANGELOG.md";
                    string? changelogContent = null;
                    if (FileExists(changelogFile))
                        changelogContent = changelogFile.ReadChangeLog().EncodeMsBuildProperty();

                    for (int i = 0; i < 2; i++)
                    {
                        DotNetBuild(s => s
                            .SetProjectFile(project)
                            .SetConfiguration(Configuration)
                            .EnableDeterministic()
                            .SetCopyright($"Copyright (c) MicroElements {DateTime.Today:yyyy}")
                            .SetAuthors("alexey.petriashev, MicroElements".EncodeMsBuildProperty())
                            .SetPackageIconUrl("https://raw.githubusercontent.com/micro-elements/MicroElements/master/image/logo_rounded.png")

                            // PackageReleaseNotes
                            .If(() => changelogContent != null,
                                settings => settings.SetPackageReleaseNotes(changelogContent))

                            // Repo settings
                            .SetRepositoryType("git")
                            .SetRepositoryUrl(GitRepository.HttpsUrl)
                            .SetPackageProjectUrl($"https://github.com/{GitRepository.Identifier}")
                            .SetProperty("RepositoryBranch", GitRepository.Branch)
                            .SetProperty("RepositoryCommit", GitRepository.Commit)

                            // License
                            .ResetPackageLicenseUrl()
                            .SetProperty("PackageLicenseExpression", "MIT"));

                        

                        // Render README from template (should be after build because uses xml documentation)
                        var isReadmeChanged = Templates.TryRenderProjectReadme(Context, projectConventions);
                        if (isReadmeChanged)
                        {
                            // Build one more time because README should be injected in result package.
                            continue;
                        }

                        break;
                    }
                }

                Context = Context with {ProjectConventions = projectConventionsList};
            });

        Target Test => _ => _
            .DependsOn(BuildAndPack)
            .Produces(TestResultsDirectory / "*.trx")
            .Executes(() =>
            {
                // TODO: see Nuke.Components.ITest

                foreach (var projectName in TestProjects)
                {
                    var testProject = Solution.GetProject(projectName);
                    DotNetTest(s => s
                        .SetProjectFile(testProject)
                        .SetConfiguration(Configuration)
                        .When(PublishTestResults, oo => oo
                            .SetLoggers("trx")
                            .SetResultsDirectory(TestResultsDirectory)));
                }
            });

        Target Docs => _ => _
            .DependsOn(BuildAndPack)
            .Executes(() =>
            {
                // Render README from template (should be after build because uses xml documentation)
                var isReadmeChanged = Templates.TryRenderSharedReadme(Context);
            });

        Target Push => _ => _
            .DependsOn(Test)
            .Requires(() => UPLOAD_NUGET)
            .Requires(() => UPLOAD_NUGET_API_KEY)
            .Requires(() => Configuration.Equals(Configuration.Release))
            .Executes(() =>
            {
                var packages = GlobFiles(ArtifactsDirectory, "*.nupkg").NotEmpty();

                foreach (var fileName in packages)
                {
                    var packageId = Path.GetFileNameWithoutExtension(fileName);
                    bool shouldUpload = packageId.IsMatchesPattern(UPLOAD_PATTERN);

                    if (!shouldUpload)
                    {
                        Info($"PackageId '{packageId}' is not matches {nameof(UPLOAD_PATTERN)} '{UPLOAD_PATTERN}'. Upload skipped.");
                        continue;
                    }

                    DotNetNuGetPush(s => s
                        .SetTargetPath(fileName)
                        .SetSource(UPLOAD_NUGET)
                        .SetApiKey(UPLOAD_NUGET_API_KEY)
                    );
                }
            });

    }

    #region Stuff

    public static class BuildExtensions
    {
        public static bool IsMatchesPattern(this string item, string? pattern) => string.IsNullOrWhiteSpace(pattern) || pattern.Contains(item, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Encodes MSBuild special characters
        /// See: https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters?view=vs-2019
        /// </summary>
        public static string EncodeMsBuildProperty(this string value) => value
            .Replace("%", "%25")
            .Replace("$", "%24")
            .Replace("@", "%40")
            .Replace("'", "%27")
            .Replace("(", "%28")
            .Replace(")", "%29")
            .Replace(";", "%3B")
            .Replace(",", "%2C")
            .Replace(" ", "%20")
            .Replace("\r", "%0D")
            .Replace("\n", "%0A")
            .Replace("\"", "%22");

        public static DotNetBuildSettings If(this DotNetBuildSettings settings, Func<bool> predicate, Func<DotNetBuildSettings, DotNetBuildSettings> configure)
        {
            if (predicate())
                return configure(settings);
            return settings;
        }

        public static string ReadChangeLog(this AbsolutePath changeLogFile)
        {
            var releaseNotes = File.ReadAllText(changeLogFile);
            return releaseNotes;
        }

        public static bool FileExists(this AbsolutePath? absolutePath) => File.Exists(absolutePath);
    }

    [TypeConverter(typeof(TypeConverter<Configuration>))]
    public class Configuration : Enumeration
    {
        public static Configuration Debug = new Configuration { Value = nameof(Debug) };
        public static Configuration Release = new Configuration { Value = nameof(Release) };

        public static implicit operator string(Configuration configuration) => configuration.Value;
    }

    public record BuildContext
    {
        public Dictionary<string, object> Context { get; init; }

        public SolutionConventions SolutionConventions { get; init; }

        public IReadOnlyCollection<ProjectConventions> ProjectConventions { get; init; }
    }

    public class SolutionConventions : IMetadataProvider
    {
        public Solution Solution { get; }

        public SolutionConventions(Solution solution)
        {
            Solution = solution;
        }

        public AbsolutePath SolutionDirectory => Solution.Directory;

        public AbsolutePath SourceDirectory => SolutionDirectory / "src";

        public AbsolutePath TestsDirectory => SolutionDirectory / "tests";

        public AbsolutePath ArtifactsDirectory => SolutionDirectory / "artifacts";

        public AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";

        public AbsolutePath ReadmeFile => SolutionDirectory / "README.md";

        public AbsolutePath ReadmeTemplateFile => SolutionDirectory / "README.md.liquid";
    }

    public class ProjectConventions : IMetadataProvider
    {
        public Project Project { get; }

        public Configuration Configuration { get; }

        public ProjectConventions(Project project, Configuration configuration)
        {
            Project = project;
            Configuration = configuration;
        }

        public AbsolutePath ProjectDirectory => Project.Directory;

        public AbsolutePath XmlDocumentationFile => ProjectDirectory / "bin" / Configuration / Project.GetTargetFrameworks()?.First() / $"{Project.Name}.xml";

        public AbsolutePath ReadmeFile => ProjectDirectory / "README.md";

        public AbsolutePath ReadmeTemplateFile => ProjectDirectory / "README.md.liquid";
    }

    #endregion

    #region BuildMetadata

    /// <summary>
    /// Represents xml documentation for project.
    /// </summary>
    public interface IProjectXmlDocumentation
    {
        /// <summary>
        /// Xml documentation for project.
        /// </summary>
        XDocument Document { get; }
    }

    /// <summary>
    /// Represents project properties.
    /// </summary>
    public interface IProjectProperties
    {
        IReadOnlyDictionary<string, string> Properties { get; }
    }

    /// <summary>
    /// Represents xml documentation for project.
    /// </summary>
    public record ProjectXmlDocumentation(XDocument Document) : IProjectXmlDocumentation;

    /// <summary>
    /// Represents project properties.
    /// </summary>
    public record ProjectProperties(IReadOnlyDictionary<string, string> Properties) : IProjectProperties;

    #endregion
}

#region Templates

namespace MicroElements.Build
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Fluid;
    using Fluid.Ast;
    using Metadata;
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
            return context.AmbientValues.TryGetValue(name, out var value) ? (T)value : default;
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

                        if (context.GetAmbientValue<TemplateData>($"template_{templateName}") is { } template)
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
                if (summaryById is not null)
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

                for (int i = lines.Length - 1; i > 0; i--)
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
                    if (i != lastNotEmpty)
                        stringBuilder.AppendLine();
                }

                string summaryContent = stringBuilder.ToString();
                return summaryContent;
            }

            return null;
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
}

#endregion
