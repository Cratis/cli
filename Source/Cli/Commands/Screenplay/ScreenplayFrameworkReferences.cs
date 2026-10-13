// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Restores target-framework reference assemblies that an MSBuild workspace can omit for projects targeting an
/// earlier installed .NET version than the CLI process.
/// </summary>
static class ScreenplayFrameworkReferences
{
    static readonly string[] _packs = ["Microsoft.NETCore.App.Ref", "Microsoft.AspNetCore.App.Ref"];

    /// <summary>
    /// Adds missing target-framework references to a project compilation.
    /// </summary>
    /// <param name="project">The workspace project.</param>
    /// <param name="compilation">The compilation produced by the workspace.</param>
    /// <returns>The original compilation when its framework is complete; otherwise, a compilation with references.</returns>
    public static Compilation AddMissingTo(Project project, Compilation compilation)
    {
        if (compilation.GetSpecialType(SpecialType.System_Object).TypeKind != TypeKind.Error)
        {
            return compilation;
        }

        var targetFramework = TargetFrameworkOf(project);
        if (targetFramework is null)
        {
            return compilation;
        }

        var existing = compilation.References
            .Select(_ => Path.GetFileNameWithoutExtension(_.Display))
            .Where(_ => !string.IsNullOrEmpty(_))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var references = PackRoots()
            .SelectMany(root => _packs.Select(pack => Path.Combine(root, pack)))
            .Where(Directory.Exists)
            .Select(pack => ReferenceDirectory(pack, targetFramework))
            .Where(_ => _ is not null)
            .SelectMany(_ => Directory.EnumerateFiles(_!, "*.dll", SearchOption.TopDirectoryOnly))
            .Where(_ => existing.Add(Path.GetFileNameWithoutExtension(_)))
            .Select(_ => MetadataReference.CreateFromFile(_))
            .ToArray();

        return references.Length == 0 ? compilation : compilation.AddReferences(references);
    }

    /// <summary>
    /// Gets the target framework selected for an MSBuild project instance.
    /// </summary>
    /// <param name="project">The selected project.</param>
    /// <returns>The target-framework moniker when it can be resolved.</returns>
    internal static string? TargetFrameworkOf(Project project)
    {
        var assemblyPath = project.CompilationOutputInfo.AssemblyPath;
        if (!string.IsNullOrWhiteSpace(assemblyPath))
        {
            var framework = new DirectoryInfo(Path.GetDirectoryName(assemblyPath)!).Name;
            if (framework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            {
                return framework;
            }
        }

        var start = project.Name.LastIndexOf('(');
        return start > 0 && project.Name.EndsWith(')') ? project.Name[(start + 1)..^1] : null;
    }

    /// <summary>
    /// Resolves SDK pack locations independently of the runtime bundled with a self-contained host.
    /// </summary>
    /// <param name="dotnetRoot">The configured .NET root, if any.</param>
    /// <param name="path">The executable search path.</param>
    /// <param name="runtimeDirectory">The current runtime directory, used as a last resort.</param>
    /// <param name="home">The user profile directory containing the NuGet package cache.</param>
    /// <returns>The candidate SDK packs directories in precedence order, followed by the user package cache.</returns>
    internal static IEnumerable<string> PackRoots(string? dotnetRoot, string? path, string runtimeDirectory, string home)
    {
        // Every candidate is offered in precedence order: a DOTNET_ROOT without packs, or a dotnet wrapper on PATH,
        // must not hide the SDK a later candidate points at. Roots without packs are skipped by the caller.
        string?[] candidates =
        [
            string.IsNullOrWhiteSpace(dotnetRoot) ? null : dotnetRoot,
            DotnetRootOnPath(path),
            new DirectoryInfo(runtimeDirectory).Parent?.Parent?.Parent?.FullName
        ];
        var roots = candidates
            .OfType<string>()
            .Select(root => Path.Combine(Path.GetFullPath(root), "packs"))
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var root in roots)
        {
            yield return root;
        }

        if (!string.IsNullOrWhiteSpace(home))
        {
            yield return Path.Combine(home, ".nuget", "packages");
        }
    }

    static IEnumerable<string> PackRoots() => PackRoots(
        Environment.GetEnvironmentVariable("DOTNET_ROOT"),
        Environment.GetEnvironmentVariable("PATH"),
        RuntimeEnvironment.GetRuntimeDirectory(),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    static string? DotnetRootOnPath(string? path)
    {
        var executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        var candidate = (path ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, executable))
            .FirstOrDefault(File.Exists);
        if (candidate is null)
        {
            return null;
        }

        var file = new FileInfo(candidate);
        var resolved = file.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? file.FullName;

        return Path.GetDirectoryName(resolved);
    }

    static string? ReferenceDirectory(string pack, string targetFramework) => Directory.EnumerateDirectories(pack)
        .Select(version => Path.Combine(version, "ref", targetFramework))
        .Where(Directory.Exists)
        .OrderDescending(StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault();
}
