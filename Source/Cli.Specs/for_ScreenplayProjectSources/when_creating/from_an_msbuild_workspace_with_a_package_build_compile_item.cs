// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Cli.for_ScreenplayProjectSources.when_creating;

/// <summary>
/// A package whose <c language="csharp">build</c> targets add a <c language="csharp">Compile</c> item from inside the
/// package rather than through <c language="csharp">contentFiles</c> - the shape <c language="csharp">Microsoft.NET.Test.Sdk</c>
/// uses for its generated entry point, which made every project referencing it fail with CLI0017.
/// </summary>
[Collection(CliSpecsCollection.Name)]
public class from_an_msbuild_workspace_with_a_package_build_compile_item : Specification
{
    string _fixtureRoot;
    Exception _error;
    SyntaxTree _packageSyntaxTree;
    IReadOnlySet<SyntaxTree> _authoredSyntaxTrees;
    ScreenplayProjectSource _source;

    async Task Because()
    {
        _fixtureRoot = Path.Combine(Path.GetTempPath(), $"screenplay-package-build-{Guid.NewGuid():N}");
        var feed = Path.Combine(_fixtureRoot, "feed");
        var checkout = Path.Combine(_fixtureRoot, "checkout");
        Directory.CreateDirectory(feed);
        Directory.CreateDirectory(checkout);
        CreatePackage(feed);
        var projectPath = await CreateProject(checkout, feed);
        await Restore(projectPath, Path.Combine(checkout, "NuGet.Config"), Path.Combine(_fixtureRoot, "packages"));

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        var compilation = await project.GetCompilationAsync() ?? throw new PackageBuildFixtureFailed("The fixture produced no compilation");
        _packageSyntaxTree = compilation.SyntaxTrees.Single(tree => Path.GetFileName(tree.FilePath) == "Entry.cs");
        _error = await Catch.Exception(async () =>
            (_authoredSyntaxTrees, _source) = await ScreenplayProjectSources.Create(project, compilation, checkout, usesWorkspaceDisplayRoot: false, CancellationToken.None));
    }

    [Fact] void should_map_the_project() => _error.ShouldBeNull();
    [Fact] void should_exclude_the_package_document_from_authored_sources() => _authoredSyntaxTrees.ShouldNotContain(_packageSyntaxTree);
    [Fact] void should_map_the_application_document() => _source.SourceContext.Files.Values.Select(_ => _.Identity.Path).ShouldContain("Order.cs");
    [Fact] void should_not_expose_the_package_document() => _source.SourceContext.Files.Values.Select(_ => Path.GetFileName(_.Identity.Path)).ShouldNotContain("Entry.cs");

    void Destroy()
    {
        if (!string.IsNullOrEmpty(_fixtureRoot) && Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
    }

    static void CreatePackage(string feed)
    {
        using var archive = ZipFile.Open(Path.Combine(feed, "Fixture.BuildItems.1.0.0.nupkg"), ZipArchiveMode.Create);
        WriteEntry(
            archive,
            "Fixture.BuildItems.nuspec",
            string.Join(
                Environment.NewLine,
                [
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                    "<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\">",
                    "  <metadata>",
                    "    <id>Fixture.BuildItems</id>",
                    "    <version>1.0.0</version>",
                    "    <authors>Cratis</authors>",
                    "    <description>Screenplay package build compile item fixture</description>",
                    "  </metadata>",
                    "</package>"
                ]));
        WriteEntry(archive, "build/Entry.cs", "internal static class FixtureEntry { }");
        WriteEntry(
            archive,
            "build/Fixture.BuildItems.props",
            string.Join(
                Environment.NewLine,
                [
                    "<Project>",
                    "  <ItemGroup>",
                    "    <Compile Include=\"$(MSBuildThisFileDirectory)Entry.cs\" Visible=\"false\" />",
                    "  </ItemGroup>",
                    "</Project>"
                ]));
    }

    static void WriteEntry(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open());
        writer.Write(content);
    }

    static async Task<string> CreateProject(string checkout, string feed)
    {
        var projectPath = Path.Combine(checkout, "Application.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            string.Join(
                Environment.NewLine,
                [
                    "<Project Sdk=\"Microsoft.NET.Sdk\">",
                    "  <PropertyGroup>",
                    "    <TargetFramework>net10.0</TargetFramework>",
                    "    <ImplicitUsings>disable</ImplicitUsings>",
                    "  </PropertyGroup>",
                    "  <ItemGroup>",
                    "    <PackageReference Include=\"Fixture.BuildItems\" Version=\"1.0.0\" />",
                    "  </ItemGroup>",
                    "</Project>"
                ]));
        await File.WriteAllTextAsync(Path.Combine(checkout, "Order.cs"), "public record Order(string Number);");
        await File.WriteAllTextAsync(
            Path.Combine(checkout, "NuGet.Config"),
            string.Join(
                Environment.NewLine,
                [
                    "<configuration>",
                    "  <packageSources>",
                    "    <clear />",
                    $"    <add key=\"fixture\" value=\"{feed}\" />",
                    "  </packageSources>",
                    "</configuration>"
                ]));
        return projectPath;
    }

    static async Task Restore(string projectPath, string configurationPath, string packageCache)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(projectPath),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { "restore", projectPath, "--configfile", configurationPath, "--packages", packageCache })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new PackageBuildFixtureFailed("The fixture restore process could not be started");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new PackageBuildFixtureFailed($"The fixture restore failed: {await output}{await error}");
        }
    }
}

sealed class PackageBuildFixtureFailed(string message) : Exception(message);
