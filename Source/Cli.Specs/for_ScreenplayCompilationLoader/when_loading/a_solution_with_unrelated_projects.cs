// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_solution_with_unrelated_projects : Specification
{
    string _root;
    string _solutionPath;
    LoadedCompilation _result;

    async Task Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-solution-regression-{Guid.NewGuid():N}")).FullName;
        WriteProject("Application/Application.csproj");
        WriteProject("Unrelated/Unrelated.csproj");
        _solutionPath = Path.Combine(_root, "Application.slnx");
        WriteSolution(_solutionPath);
        await Restore(_solutionPath);
    }

    async Task Because() => _result = await ScreenplayCompilationLoader.Load(
        _solutionPath,
        includeAllProjects: true,
        CancellationToken.None);

    [Fact] void should_keep_solution_wide_project_selection() => _result.ProjectNames.ShouldContainOnly(["Application", "Unrelated"]);
    [Fact] void should_keep_solution_project_order() => _result.ProjectNames.SequenceEqual(["Application", "Unrelated"]).ShouldBeTrue();
    [Fact] void should_keep_solution_workspace_relative_paths() => _result.ProjectSources.Select(_ => _.LogicalProjectPath).ShouldEqual(["Application/Application.csproj", "Unrelated/Unrelated.csproj"]);
    [Fact] void should_keep_solution_workspace_display_roots() => _result.ProjectSources.All(_ => _.SourceContext.Policy.DisplayRoot == DotNetSourceDisplayRoot.Workspace).ShouldBeTrue();

    void Destroy()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    static void WriteSolution(string path) => File.WriteAllText(
        path,
        $"<Solution>{Environment.NewLine}  <Project Path=\"Application/Application.csproj\" />{Environment.NewLine}  <Project Path=\"Unrelated/Unrelated.csproj\" />{Environment.NewLine}</Solution>{Environment.NewLine}");

    void WriteProject(string relativePath)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>{Environment.NewLine}</Project>{Environment.NewLine}");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "Source.cs"), $"namespace {Path.GetFileNameWithoutExtension(path)}; public sealed class Marker;{Environment.NewLine}");
    }

    static async Task Restore(string solutionPath)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(solutionPath),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(solutionPath);
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
