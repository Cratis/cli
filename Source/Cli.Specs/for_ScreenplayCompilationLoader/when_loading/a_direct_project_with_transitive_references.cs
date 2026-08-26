// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_direct_project_with_transitive_references : Specification
{
    string _root;
    string _targetPath;
    LoadedCompilation _result;
    IReadOnlyList<DotNetProjectCompilation> _mapped;

    async Task Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-direct-closure-{Guid.NewGuid():N}")).FullName;
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        WriteProject("Dependencies/Core/Core.csproj");
        WriteProject("Dependencies/Shared/Shared.csproj", "../Core/Core.csproj");
        WriteProject("Application.Specs/Application.Specs.csproj");
        WriteProject("Unrelated/Unrelated.csproj");
        WriteProject("Reverse/Reverse.csproj", "../Host/Application.csproj");
        WriteProject("Host/Application.csproj", "../Dependencies/Shared/Shared.csproj", "../Application.Specs/Application.Specs.csproj");
        _targetPath = Path.Combine(_root, "Host", "Application.csproj");
        await Restore(_targetPath);
    }

    async Task Because()
    {
        _result = await ScreenplayCompilationLoader.Load(_targetPath, includeAllProjects: true, CancellationToken.None);
        _mapped = ScreenplayProjectCompilations.From(_result, _targetPath);
    }

    [Fact] void should_load_the_direct_and_transitive_closure() => _result.ProjectNames.SequenceEqual(["Core", "Shared", "Application"]).ShouldBeTrue();
    [Fact] void should_exclude_spec_projects() => _result.ProjectNames.ShouldNotContain("Application.Specs");
    [Fact] void should_exclude_unrelated_projects() => _result.ProjectNames.ShouldNotContain("Unrelated");
    [Fact] void should_exclude_reverse_projects() => _result.ProjectNames.ShouldNotContain("Reverse");
    [Fact] void should_order_logical_project_paths_deterministically() => _result.ProjectSources.Select(_ => _.LogicalProjectPath).ShouldEqual(["Dependencies/Core/Core.csproj", "Dependencies/Shared/Shared.csproj", "Host/Application.csproj"]);
    [Fact] void should_display_direct_multi_project_sources_from_the_workspace() => _result.ProjectSources.All(_ => _.SourceContext.Policy.DisplayRoot == DotNetSourceDisplayRoot.Workspace).ShouldBeTrue();
    [Fact] void should_assign_application_roles_explicitly() => _result.ProjectSources.All(_ => _.Role == DotNetProjectRole.Application).ShouldBeTrue();
    [Fact] void should_align_every_project_name() => _mapped.Select(_ => _.Name).ShouldEqual(_result.ProjectNames);
    [Fact] void should_align_every_project_role() => _mapped.Select(_ => _.Role).ShouldEqual(_result.ProjectSources.Select(_ => _.Role));
    [Fact] void should_align_every_project_path() => _mapped.Select(_ => _.ProjectPath).ShouldEqual(_result.ProjectSources.Select(_ => _.ProjectPath));
    [Fact] void should_align_every_workspace_root() => _mapped.Select(_ => _.SourceRoot).ShouldEqual(_result.ProjectSources.Select(_ => _.SourceRoot));
    [Fact] void should_align_every_source_context() => _mapped.Select(_ => _.SourceContext).ShouldEqual(_result.ProjectSources.Select(_ => _.SourceContext));
    [Fact] void should_align_every_compilation() => _mapped.Select(_ => _.Compilation).ShouldEqual(_result.Compilations);
    [Fact] void should_align_every_authored_tree_set() => _mapped.Select(_ => _.AuthoredSyntaxTrees).ShouldEqual(_result.AuthoredSyntaxTrees);
    [Fact] void should_use_the_nearest_repository_root_for_every_project() => _result.ProjectSources.All(_ => _.SourceRoot == ScreenplayProjectSources.CanonicalPathOf(_root)).ShouldBeTrue();

    void Destroy()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    void WriteProject(string relativePath, params string[] projectReferences)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var references = string.Join(Environment.NewLine, projectReferences.Select(reference => $"    <ProjectReference Include=\"{reference}\" />"));
        var itemGroup = projectReferences.Length == 0 ? string.Empty : $"  <ItemGroup>{Environment.NewLine}{references}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        File.WriteAllText(
            path,
            $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>{Environment.NewLine}{itemGroup}</Project>{Environment.NewLine}");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "Source.cs"), $"namespace {Path.GetFileNameWithoutExtension(path).Replace('.', '_')}; public sealed class Marker;{Environment.NewLine}");
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

sealed class DirectProjectRestoreFailed(string message) : Exception(message);
