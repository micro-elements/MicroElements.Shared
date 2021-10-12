using System;
using System.ComponentModel;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;


#region Supressions

// ReSharper disable InconsistentNaming

#endregion

//*****************************************************************
// Support plugins are available for:
//  - JetBrains ReSharper        https://nuke.build/resharper
//   - JetBrains Rider            https://nuke.build/rider
//   - Microsoft VisualStudio     https://nuke.build/visualstudio
//   - Microsoft VSCode           https://nuke.build/vscode
//*****************************************************************

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.BuildFromVS);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json";
    [Parameter] string NugetApiKey;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    static readonly string[] ProjectsToBuild = new string[]
    {
        "MicroElements.JetBrains.Sources",
        "MicroElements.CodeContracts.Sources",
        "MicroElements.Extensions.Collections.Sources",
        "MicroElements.Formatting.Sources",
        "MicroElements.Reflection",
    };

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target BuildFromVS => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            foreach (var projectToBuild in ProjectsToBuild)
            {
                var project = Solution.GetProject(projectToBuild);
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                );
            }

            Solution.Save();
        });

    Target Pack => _ => _
        .Produces(ArtifactsDirectory / "*.nupkg")
        .DependsOn(Clean)
        .Executes(() =>
        {
            foreach (var projectToBuild in ProjectsToBuild)
            {
                var project = Solution.GetProject(projectToBuild);
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                );
            }
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => NugetApiUrl)
        .Requires(() => NugetApiKey)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            GlobFiles(ArtifactsDirectory, "*.nupkg")
                .NotEmpty()
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .ForEach(x =>
                {
                    DotNetNuGetPush(s => s
                        .SetTargetPath(x)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey)
                    );
                });
        });

    Target GitHubActions => _ => _
        .DependsOn(Pack)
        .Executes();
}

#region Stuff

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration
{
    public static Configuration Debug = new Configuration { Value = nameof(Debug) };
    public static Configuration Release = new Configuration { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}

#endregion
