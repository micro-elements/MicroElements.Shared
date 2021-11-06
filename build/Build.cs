using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using MicroElements.Build;
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

partial class Build
{
    public static int Main() => Execute<Build>(x => x.BuildDocs);

    Target BuildFromVS => _ => _
        .DependsOn(BuildAndPack, Test)
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
partial class Build : NukeBuild, ITest
{
    static readonly string[] ProjectsToBuild = new string[]
    {
        "MicroElements.IsExternalInit",
        "MicroElements.JetBrains.Sources",
        "MicroElements.CodeContracts.Sources",
        "MicroElements.Collections.Sources",
        "MicroElements.Formatting.Sources",
        "MicroElements.Reflection.Sources",
        "MicroElements.Reflection",
        "MicroElements.Shared.Sources",
    };

    static readonly string[] TestProjects = new string[]
    {
        "MicroElements.Shared.Tests",
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

[PublicAPI]
public interface IHazArtifacts : INukeBuild
{
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
}

[PublicAPI]
public interface ITest : IHazArtifacts
{
    public AbsolutePath TestResultDirectory => ArtifactsDirectory / "test-results";
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
