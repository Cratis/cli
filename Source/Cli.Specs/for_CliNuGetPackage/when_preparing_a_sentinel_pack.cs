// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliNuGetPackage;

public class when_preparing_a_sentinel_pack
{
    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Release", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", true)]
    public void should_pack_the_source_project_with_its_resolved_build_context(string configuration, bool useArtifacts)
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var projectPath = Path.Combine(root, "tested checkout", "Source", "Cli", "Cli.csproj");
        var artifactsPath = useArtifacts ? Path.Combine(root, "other checkout", "artifacts") : string.Empty;
        var versionsPath = Path.Combine(root, "candidate versions.props");
        var outputPath = Path.Combine(root, "sentinel package");
        var properties = new Dictionary<string, string>
        {
            ["Configuration"] = configuration,
            ["DirectoryPackagesPropsPath"] = versionsPath,
            ["ArtifactsPath"] = artifactsPath,
            ["UseArtifactsOutput"] = useArtifacts ? "true" : string.Empty,
            ["RuntimeIdentifier"] = string.Empty,
            ["TreatWarningsAsErrors"] = "true"
        };

        var startInfo = SentinelPackageBuild.CreateStartInfo(projectPath, properties, outputPath, "0.0.0-sentinel");

        startInfo.FileName.ShouldEqual("dotnet");
        startInfo.WorkingDirectory.ShouldEqual(Path.GetDirectoryName(projectPath));
        startInfo.UseShellExecute.ShouldBeFalse();
        startInfo.RedirectStandardOutput.ShouldBeTrue();
        startInfo.RedirectStandardError.ShouldBeTrue();
        var expected = new List<string>
        {
            "pack", projectPath, "--no-restore", "--output", outputPath,
            "-p:PackageVersion=0.0.0-sentinel",
            $"-p:Configuration={configuration}",
            $"-p:DirectoryPackagesPropsPath={versionsPath}"
        };
        if (useArtifacts)
        {
            expected.Add($"-p:ArtifactsPath={artifactsPath}");
            expected.Add("-p:UseArtifactsOutput=true");
        }
        expected.Add("-p:TreatWarningsAsErrors=true");
        startInfo.ArgumentList.SequenceEqual(expected).ShouldBeTrue();
    }
}
