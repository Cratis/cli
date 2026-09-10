// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_direct_project_with_a_dependency_outside_the_nearest_git_root : Specification
{
    string _root;
    string _targetPath;
    LoadedCompilation _result;

    async Task Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-direct-outside-git-{Guid.NewGuid():N}")).FullName;
        var repositoryRoot = Directory.CreateDirectory(Path.Combine(_root, "Repository")).FullName;
        Directory.CreateDirectory(Path.Combine(repositoryRoot, ".git"));
        WriteProject(Path.Combine(_root, "Dependency", "Dependency.csproj"));
        _targetPath = Path.Combine(repositoryRoot, "Host", "Application.csproj");
        WriteProject(_targetPath, "../../Dependency/Dependency.csproj");
        await Restore(_targetPath);
    }

    async Task Because() => _result = await ScreenplayCompilationLoader.Load(
        _targetPath,
        includeAllProjects: true,
        CancellationToken.None);

    [Fact] void should_fail_with_the_invalid_source_path_code() => _result.Diagnostics.Single(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).Code.ShouldEqual(ScreenplayDiagnosticCodes.InvalidSourcePath);
    [Fact] void should_use_the_logical_target_identity() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.InvalidSourcePath).Location.ShouldEqual("Application.csproj");
    [Fact] void should_not_leak_the_physical_workspace_root() => JsonSerializer.Serialize(_result.Diagnostics).ShouldNotContain(_root);
    [Fact] void should_create_no_compilations() => _result.Compilations.ShouldBeEmpty();
    [Fact] void should_create_no_project_sources() => _result.ProjectSources.ShouldBeEmpty();

    void Destroy()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    static void WriteProject(string path, params string[] projectReferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var references = string.Join(Environment.NewLine, projectReferences.Select(reference => $"    <ProjectReference Include=\"{reference}\" />"));
        var itemGroup = projectReferences.Length == 0 ? string.Empty : $"  <ItemGroup>{Environment.NewLine}{references}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        File.WriteAllText(
            path,
            $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>{Environment.NewLine}{itemGroup}</Project>{Environment.NewLine}");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "Source.cs"), $"namespace {Path.GetFileNameWithoutExtension(path)}; public sealed class Marker;{Environment.NewLine}");
    }

    static async Task Restore(string projectPath)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(projectPath),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(projectPath);
        using var process = Process.Start(startInfo) ?? throw new DirectProjectRestoreFailed("Could not start dotnet restore.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new DirectProjectRestoreFailed($"dotnet restore failed.{Environment.NewLine}{await output}{Environment.NewLine}{await error}");
        }
    }
}
