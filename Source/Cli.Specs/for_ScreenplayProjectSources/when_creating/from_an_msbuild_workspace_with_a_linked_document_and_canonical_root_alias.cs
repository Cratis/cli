// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cratis.Cli.for_ScreenplayProjectSources.when_creating;

/// <summary>
/// Characterizes the macOS temporary-root alias through a real MSBuild workspace without naming either physical root.
/// </summary>
/// <remarks>
/// The host gate is intentional: non-macOS agents, or macOS hosts whose temporary path has no symbolic-link alias,
/// cannot exercise the canonical-root mismatch, so this characterization makes no assertions rather than simulating it.
/// </remarks>
[Collection(CliSpecsCollection.Name)]
public class from_an_msbuild_workspace_with_a_linked_document_and_canonical_root_alias : Specification
{
    string _aliasRoot;
    string _canonicalRoot;
    ScreenplayProjectSource _source;
    bool _isApplicable;

    void Establish() => ScreenplayCompilationLoader.RegisterMSBuild();

    [MethodImpl(MethodImplOptions.NoInlining)]
    async Task Because()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var aliasTemporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        var canonicalTemporaryRoot = CanonicalTemporaryRootOf(aliasTemporaryRoot);
        if (canonicalTemporaryRoot is null)
        {
            return;
        }

        _isApplicable = true;

        var fixture = $"screenplay-msbuild-{Guid.NewGuid():N}";
        _aliasRoot = Path.Combine(aliasTemporaryRoot, fixture);
        _canonicalRoot = Path.Combine(canonicalTemporaryRoot, fixture);
        var projectDirectory = Path.Combine(_canonicalRoot, "Source", "Application");
        var sharedDirectory = Path.Combine(_canonicalRoot, "Shared");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(sharedDirectory);
        var projectPath = Path.Combine(projectDirectory, "Application.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            string.Join(
                Environment.NewLine,
                [
                    "<Project Sdk=\"Microsoft.NET.Sdk\">",
                    "  <PropertyGroup>",
                    "    <TargetFramework>net10.0</TargetFramework>",
                    "  </PropertyGroup>",
                    "  <ItemGroup>",
                    "    <Compile Include=\"../../Shared/PlaceOrder.cs\" Link=\"Links/SharedOrder.cs\" />",
                    "  </ItemGroup>",
                    "</Project>"
                ]));
        await File.WriteAllTextAsync(Path.Combine(sharedDirectory, "PlaceOrder.cs"), "public record PlaceOrder;");

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        var compilation = await project.GetCompilationAsync() ?? throw new SourceFixtureCompilationFailed();
        _source = (await ScreenplayProjectSources.Create(
            project,
            compilation,
            _aliasRoot,
            usesWorkspaceDisplayRoot: true,
            CancellationToken.None)).Source;
    }

    [Fact] void should_map_the_canonical_project_through_the_alias_root()
    {
        if (_isApplicable)
        {
            _source.LogicalProjectPath.ShouldEqual("Source/Application/Application.csproj");
        }
    }

    [Fact] void should_display_the_linked_document_from_the_workspace_root()
    {
        if (_isApplicable)
        {
            _source.SourceContext.Files.Values.Select(_ => _.DisplayPath).ShouldContain("Shared/PlaceOrder.cs");
        }
    }

    [Fact] void should_preserve_the_linked_document_identity()
    {
        if (_isApplicable)
        {
            _source.SourceContext.Files.Values.Select(_ => _.Identity.Path).ShouldContain("Links/SharedOrder.cs");
        }
    }

    void Destroy()
    {
        if (!string.IsNullOrEmpty(_aliasRoot) && Directory.Exists(_aliasRoot))
        {
            Directory.Delete(_aliasRoot, recursive: true);
        }
    }

    static string? CanonicalTemporaryRootOf(string aliasTemporaryRoot)
    {
        var root = Path.GetPathRoot(aliasTemporaryRoot)!;
        var parts = aliasTemporaryRoot[root.Length..].Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        var target = new DirectoryInfo(Path.Combine(root, parts[0])).ResolveLinkTarget(returnFinalTarget: true);
        return target is null
            ? null
            : parts.Skip(1).Aggregate(target.FullName, Path.Combine);
    }
}
