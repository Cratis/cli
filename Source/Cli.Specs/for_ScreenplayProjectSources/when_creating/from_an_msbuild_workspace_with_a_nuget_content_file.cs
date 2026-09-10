// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Cli.for_ScreenplayProjectSources.when_creating;

/// <summary>
/// Characterizes package-owned compile documents through a real MSBuild workspace.
/// </summary>
[Collection(CliSpecsCollection.Name)]
public class from_an_msbuild_workspace_with_a_nuget_content_file : Specification
{
    string _fixtureRoot;
    SourceFixture _first;
    SourceFixture _relocated;

    void Establish() => ScreenplayCompilationLoader.RegisterMSBuild();

    async Task Because()
    {
        _fixtureRoot = Path.Combine(Path.GetTempPath(), $"screenplay-package-content-{Guid.NewGuid():N}");
        var feed = Path.Combine(_fixtureRoot, "feed");
        Directory.CreateDirectory(feed);
        CreatePackage(feed);

        _first = await SourceFrom(
            Path.Combine(_fixtureRoot, "checkouts", "first"),
            Path.Combine(_fixtureRoot, "packages", "first"),
            feed);
        _relocated = await SourceFrom(
            Path.Combine(_fixtureRoot, "checkouts", "relocated"),
            Path.Combine(_fixtureRoot, "packages", "relocated"),
            feed);
    }

    [Fact] void should_keep_the_package_tree_in_the_compilation() => _first.CompilationContainsPackageTree.ShouldBeTrue();
    [Fact] void should_compile_with_the_package_global_using() => _first.CompilationErrors.ShouldBeEmpty();
    [Fact] void should_exclude_the_package_tree_from_authored_sources() => _first.AuthoredSyntaxTrees.ShouldNotContain(_first.PackageSyntaxTree);
    [Fact] void should_map_the_application_document() => _first.Source.SourceContext.Files.Values.Select(_ => _.Identity.Path).ShouldContain("Order.cs");
    [Fact] void should_not_expose_the_package_document() => _first.Source.SourceContext.Files.Values.Select(_ => _.Identity.Path).ShouldNotContain("GlobalUsings.cs");
    [Fact] void should_use_a_different_physical_package_path_after_relocation() => _relocated.PackageDocumentPath.ShouldNotEqual(_first.PackageDocumentPath);
    [Fact] void should_preserve_source_metadata_after_relocating_the_checkout_and_package_cache() => LogicalDescription(_relocated.Source).ShouldEqual(LogicalDescription(_first.Source));

    void Destroy()
    {
        if (!string.IsNullOrEmpty(_fixtureRoot) && Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
    }

    static void CreatePackage(string feed)
    {
        var packagePath = Path.Combine(feed, "Fixture.ContentFiles.1.0.0.nupkg");
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create);
        WriteEntry(
            archive,
            "Fixture.ContentFiles.nuspec",
            string.Join(
                Environment.NewLine,
                [
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                    "<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\">",
                    "  <metadata>",
                    "    <id>Fixture.ContentFiles</id>",
                    "    <version>1.0.0</version>",
                    "    <authors>Cratis</authors>",
                    "    <description>Screenplay package content fixture</description>",
                    "    <contentFiles>",
                    "      <files include=\"cs/any/GlobalUsings.cs\" buildAction=\"None\" />",
                    "    </contentFiles>",
                    "  </metadata>",
                    "</package>"
                ]));
        WriteEntry(archive, "contentFiles/cs/any/GlobalUsings.cs", "global using System;");
        WriteEntry(
            archive,
            "buildTransitive/Fixture.ContentFiles.props",
            string.Join(
                Environment.NewLine,
                [
                    "<Project>",
                    "  <ItemGroup>",
                    "    <Compile Include=\"$(MSBuildThisFileDirectory)..\\contentFiles\\cs\\any\\GlobalUsings.cs\" Link=\"GlobalUsings.Fixture.cs\" Visible=\"false\" />",
                    "  </ItemGroup>",
                    "</Project>"
                ]));
    }

    static void WriteEntry(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open());
        writer.Write(content);
    }

    static async Task<SourceFixture> SourceFrom(string checkout, string packageCache, string feed)
    {
        Directory.CreateDirectory(checkout);
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
                    "    <PackageReference Include=\"Fixture.ContentFiles\" Version=\"1.0.0\" />",
                    "  </ItemGroup>",
                    "</Project>"
                ]));
        await File.WriteAllTextAsync(Path.Combine(checkout, "Order.cs"), "public record Order(DateTime Occurred);");
        var configurationPath = Path.Combine(checkout, "NuGet.Config");
        await File.WriteAllTextAsync(
            configurationPath,
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
        await Restore(projectPath, configurationPath, packageCache);

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        var packageDocument = project.Documents.Single(document =>
            string.Equals(Path.GetFileName(document.FilePath), "GlobalUsings.cs", StringComparison.Ordinal));
        var packageSyntaxTree = await packageDocument.GetSyntaxTreeAsync() ?? throw new SourceFixtureCompilationFailed();
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();
        var (authoredSyntaxTrees, source) = await ScreenplayProjectSources.Create(
            project,
            compilation,
            checkout,
            usesWorkspaceDisplayRoot: false,
            CancellationToken.None);

        return new SourceFixture(
            source,
            authoredSyntaxTrees,
            packageSyntaxTree,
            packageDocument.FilePath,
            compilation.SyntaxTrees.Contains(packageSyntaxTree),
            [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)]);
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
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configfile");
        startInfo.ArgumentList.Add(configurationPath);
        startInfo.ArgumentList.Add("--packages");
        startInfo.ArgumentList.Add(packageCache);

        using var process = Process.Start(startInfo) ?? throw new PackageContentFixtureRestoreFailed("The fixture restore process could not be started");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(standardOutput, standardError);
        var output = await standardOutput;
        var error = await standardError;
        if (process.ExitCode != 0)
        {
            throw new PackageContentFixtureRestoreFailed($"The fixture restore failed: {output}{error}");
        }
    }

    static string LogicalDescription(ScreenplayProjectSource source)
    {
        var files = source.SourceContext.Files.Values
            .OrderBy(file => file.Identity.Path, StringComparer.Ordinal)
            .Select(file => $"{file.Identity}|{file.DisplayPath}");
        return $"{source.LogicalProjectPath}|{source.SourceContext.ProjectIdentity}|{source.SourceContext.Policy.Version}|{source.SourceContext.Policy.DisplayRoot}|{source.SourceContext.Policy.CasePolicy}|{string.Join(';', files)}";
    }

    sealed record SourceFixture(
        ScreenplayProjectSource Source,
        IReadOnlySet<SyntaxTree> AuthoredSyntaxTrees,
        SyntaxTree PackageSyntaxTree,
        string PackageDocumentPath,
        bool CompilationContainsPackageTree,
        IReadOnlyList<Diagnostic> CompilationErrors);
}

sealed class PackageContentFixtureRestoreFailed(string message) : Exception(message);
