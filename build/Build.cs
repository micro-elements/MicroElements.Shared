using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Build.Tasks.Deployment.ManifestUtilities;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Logger;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

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
    public static int Main() => Execute<Build>(x => x.BuildFromVS);

    Target BuildFromVS => _ => _
        .DependsOn(BuildAndPack, Test)
        .Executes(() =>
        {
            // To properly reload projects
            Solution.Save();
        });

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

    #endregion

    [Solution] readonly Solution Solution = null!;
    [GitRepository] readonly GitRepository GitRepository = null!;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    [Parameter("PublishTestResults")]
    bool PublishTestResults = true;

    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";

    static readonly string[] ProjectsToBuild = new string[]
    {
        "MicroElements.JetBrains.Sources",
        "MicroElements.CodeContracts.Sources",
        "MicroElements.Collections.Extensions.Sources",
        "MicroElements.Formatting.Sources",
        "MicroElements.Reflection",
        "MicroElements.Shared.Sources",
    };

    static readonly string[] TestProjects = new string[]
    {
        "MicroElements.Shared.Tests",
    };

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
            foreach (var projectToBuild in ProjectsToBuild)
            {
                bool shouldBuild = projectToBuild.IsMatchesPattern(BUILD_PATTERN);
                if (!shouldBuild)
                {
                    Info($"Project '{projectToBuild}' is not matches {nameof(BUILD_PATTERN)} '{BUILD_PATTERN}'. Build skipped.");
                    continue;
                }

                var project = Solution.GetProject(projectToBuild);
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .EnableDeterministic()
                    
                    .SetCopyright($"Copyright (c) MicroElements {DateTime.Today:yyyy}")
                    .SetAuthors("alexey.petriashev, MicroElements".EncodeComma())
                    .SetPackageIconUrl("https://raw.githubusercontent.com/micro-elements/MicroElements/master/image/logo_rounded.png")

                    .SetRepositoryType("git")
                    .SetRepositoryUrl(GitRepository.HttpsUrl)
                    .SetPackageProjectUrl($"https://github.com/{GitRepository.Identifier}")

                    .ResetPackageLicenseUrl()
                    .SetProperty("PackageLicenseExpression", "MIT")
                );
            }
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
    /// Encodes comma as special symbol. Comma in property fails build.
    /// See: https://github.com/nuke-build/nuke/issues/497
    /// </summary>
    public static string EncodeComma(this string value) => value.Replace(",", "%2c");
}

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug = new Configuration { Value = nameof(Debug) };
    public static Configuration Release = new Configuration { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}

#endregion
