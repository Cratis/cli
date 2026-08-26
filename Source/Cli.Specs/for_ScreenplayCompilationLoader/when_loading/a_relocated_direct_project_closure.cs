// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_relocated_direct_project_closure : Specification
{
    string _fixtureRoot;
    LoadedCompilation _first;
    LoadedCompilation _relocated;

    async Task Establish()
    {
        _fixtureRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-direct-relocated-{Guid.NewGuid():N}")).FullName;
        await CreateWorkspace(Path.Combine(_fixtureRoot, "First"));
        await CreateWorkspace(Path.Combine(_fixtureRoot, "Relocated"));
    }

    async Task Because()
    {
        _first = await Load(Path.Combine(_fixtureRoot, "First"));
        _relocated = await Load(Path.Combine(_fixtureRoot, "Relocated"));
    }

    [Fact] void should_preserve_project_order_after_relocation() => _relocated.ProjectNames.ShouldEqual(_first.ProjectNames);
    [Fact] void should_preserve_logical_project_paths_after_relocation() => _relocated.ProjectSources.Select(_ => _.LogicalProjectPath).ShouldEqual(_first.ProjectSources.Select(_ => _.LogicalProjectPath));
    [Fact] void should_preserve_source_contexts_after_relocation() => LogicalSourceContexts(_relocated).ShouldEqual(LogicalSourceContexts(_first));
    [Fact] void should_preserve_authored_source_bytes_after_relocation() => AuthoredSourceBytes(_relocated).ShouldEqual(AuthoredSourceBytes(_first));
    [Fact] void should_change_only_internal_physical_roots_after_relocation() => _relocated.ProjectSources.Select(_ => _.SourceRoot).ShouldNotEqual(_first.ProjectSources.Select(_ => _.SourceRoot));
    [Fact] void should_not_emit_physical_roots_in_project_provenance() => JsonSerializer.Serialize(_relocated.ProjectProvenance).ShouldNotContain(_fixtureRoot);

    void Destroy()
    {
        if (Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, true);
        }
    }

    static async Task<LoadedCompilation> Load(string root) => await ScreenplayCompilationLoader.Load(
        Path.Combine(root, "Host", "Application.csproj"),
        includeAllProjects: true,
        CancellationToken.None);

    static async Task CreateWorkspace(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        WriteProject(root, "Shared/Shared.csproj");
        WriteProject(root, "Host/Application.csproj", "../Shared/Shared.csproj");
        await Restore(Path.Combine(root, "Host", "Application.csproj"));
    }

    static void WriteProject(string root, string relativePath, params string[] projectReferences)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var references = string.Join(Environment.NewLine, projectReferences.Select(reference => $"    <ProjectReference Include=\"{reference}\" />"));
        var itemGroup = projectReferences.Length == 0 ? string.Empty : $"  <ItemGroup>{Environment.NewLine}{references}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        File.WriteAllText(
            path,
            $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>{Environment.NewLine}{itemGroup}</Project>{Environment.NewLine}");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "Source.cs"), $"namespace {Path.GetFileNameWithoutExtension(path)}; public sealed class Marker;{Environment.NewLine}");
    }

    static IReadOnlyList<string> LogicalSourceContexts(LoadedCompilation loaded) =>
    [
        .. loaded.ProjectSources.Select(source =>
        {
            var files = source.SourceContext.Files.Values
                .OrderBy(file => file.Identity.Path, StringComparer.Ordinal)
                .Select(file => $"{file.Identity}|{file.DisplayPath}");
            var policy = source.SourceContext.Policy;
            return $"{source.LogicalProjectPath}|{source.SourceContext.ProjectIdentity}|{policy.Version}|{policy.DisplayRoot}|{policy.CasePolicy}|{string.Join(';', files)}";
        })
    ];

    static IReadOnlyList<string> AuthoredSourceBytes(LoadedCompilation loaded) =>
    [
        .. loaded.ProjectSources.SelectMany(source => source.SourceContext.Files
            .OrderBy(file => file.Value.Identity.Path, StringComparer.Ordinal)
            .Select(file => Convert.ToBase64String(Encoding.UTF8.GetBytes(file.Key.GetText().ToString()))))
    ];

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
