// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cratis.Affected;

/// <summary>
/// Runs xUnit v3 test projects, which are executables: dotnet &lt;assembly&gt;.dll -filter &lt;query&gt;.
/// </summary>
/// <param name="repositoryRoot">The working tree root.</param>
/// <param name="configuration">The build configuration to build and run.</param>
/// <param name="framework">The target framework to build, or null for a project with a single one.</param>
/// <param name="buildArguments">Extra arguments for every dotnet build.</param>
public partial class XunitV3Runner(string repositoryRoot, string configuration = "Release", string? framework = null, IReadOnlyList<string>? buildArguments = null) : ITestRunner
{
    [GeneratedRegex(@"^[\p{L}_][\w.+`<>,]*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TypeNamePattern { get; }

    [GeneratedRegex(@"Test method:\s+(?<name>\S+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TestMethodPattern { get; }

    /// <inheritdoc/>
    public async Task<TestAssembly> BuildAsync(string project, CancellationToken cancellationToken)
    {
        var withFramework = await BuildWithAsync(project, framework, cancellationToken);
        if (withFramework.Built.ExitCode != 0
            && framework is not null
            && withFramework.Built.Output.Contains("NETSDK1005", StringComparison.Ordinal))
        {
            // The project does not target the requested framework. A project with one framework is built for it.
            var own = await BuildWithAsync(project, await SingleFrameworkOfAsync(project, cancellationToken), cancellationToken);
            if (own.Built.ExitCode == 0)
            {
                withFramework = own;
            }
        }

        var (built, evaluated) = withFramework;
        if (built.ExitCode != 0)
        {
            throw new AffectedException($"Building {project} failed:\n{built.Output}\n{built.Error}");
        }

        if (evaluated.ExitCode != 0)
        {
            throw new AffectedException($"Reading the output path of {project} failed:\n{evaluated.Output}\n{evaluated.Error}");
        }

        return new TestAssembly(project, ParseTargetPath(evaluated.Output));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> DiscoverUnitsAsync(TestAssembly assembly, UnitGranularity granularity, CancellationToken cancellationToken)
    {
        return await List(assembly, granularity, [], cancellationToken);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IReadOnlyList<string>> CreateFilterBatches(IReadOnlyList<SelectionEntry> entries, UnitGranularity granularity, int maxLength) =>
        XunitV3Filter.CreateBatches(entries, granularity, maxLength);

    /// <inheritdoc/>
    public IReadOnlyList<string> CreateUnitFilter(string unit, UnitGranularity granularity) =>
        [.. XunitV3Filter.QueriesForUnit(unit, granularity).SelectMany(query => new[] { "-filter", query })];

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ListMatchedAsync(TestAssembly assembly, IReadOnlyList<string> arguments, UnitGranularity granularity, CancellationToken cancellationToken) =>
        List(assembly, granularity, arguments, cancellationToken);

    /// <inheritdoc/>
    public Task<int> RunAsync(TestAssembly assembly, IReadOnlyList<string> arguments, string? resultsFile, CancellationToken cancellationToken)
    {
        var command = GetCommand(assembly, arguments).ToList();
        if (resultsFile is not null)
        {
            // The runner starts in the assembly's folder, so the result path must not depend on the working directory.
            var fullPath = Path.GetFullPath(resultsFile);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            command.AddRange(["-result-trx", fullPath]);
        }

        return ProcessRunner.RunInheritedAsync(command[0], command.Skip(1), Path.GetDirectoryName(assembly.AssemblyPath)!, cancellationToken);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetCommand(TestAssembly assembly, IReadOnlyList<string> arguments) => ["dotnet", assembly.AssemblyPath, .. arguments];

    static string ParseTargetPath(string output)
    {
        var text = output.Trim();
        if (text.StartsWith('{'))
        {
            using var document = JsonDocument.Parse(text);
            var path = document.RootElement.GetProperty("Properties").GetProperty("TargetPath").GetString();
            return string.IsNullOrWhiteSpace(path)
                ? throw new AffectedException("The build reported no target path. A project that targets several frameworks needs one of them chosen with --framework.")
                : path;
        }

        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault()
            ?? throw new AffectedException("The build reported no target path.");
    }

    async Task<IReadOnlyList<string>> List(TestAssembly assembly, UnitGranularity granularity, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var kind = granularity == UnitGranularity.Test ? "full" : "classes";
        var command = GetCommand(assembly, ["-list", kind, "-noColor", .. arguments]);
        var result = await Task.Run(() => ProcessRunner.Run(command[0], command.Skip(1), Path.GetDirectoryName(assembly.AssemblyPath)!), cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new AffectedException($"Listing the tests of {assembly.Project} failed:\n{result.Output}\n{result.Error}");
        }

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var units = granularity == UnitGranularity.Test
            ? lines.Select(line => TestMethodPattern.Match(line)).Where(match => match.Success).Select(match => match.Groups["name"].Value)
            : lines.Where(line => TypeNamePattern.IsMatch(line) && !line.StartsWith("xUnit", StringComparison.Ordinal));
        return [.. units.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    async Task<string?> SingleFrameworkOfAsync(string project, CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "msbuild", project, $"-p:Configuration={configuration}", "-nologo", "-getProperty:TargetFrameworks,TargetFramework" };
        arguments.AddRange(buildArguments ?? []);
        var result = await Task.Run(() => ProcessRunner.Run("dotnet", arguments, repositoryRoot), cancellationToken);
        if (result.ExitCode != 0 || !result.Output.TrimStart().StartsWith('{'))
        {
            return null;
        }

        using var document = JsonDocument.Parse(result.Output.Trim());
        var properties = document.RootElement.GetProperty("Properties");
        var frameworks = (properties.GetProperty("TargetFrameworks").GetString() ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (frameworks.Length == 1)
        {
            return frameworks[0];
        }

        var single = properties.GetProperty("TargetFramework").GetString();
        return string.IsNullOrWhiteSpace(single) ? null : single;
    }

    async Task<(ProcessResult Built, ProcessResult Evaluated)> BuildWithAsync(string project, string? targetFramework, CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "build", project, "-c", configuration, "-nologo", "-v", "q" };
        if (targetFramework is not null)
        {
            arguments.AddRange(["-f", targetFramework]);
        }

        arguments.AddRange(buildArguments ?? []);

        var built = await Task.Run(() => ProcessRunner.Run("dotnet", arguments, repositoryRoot), cancellationToken);
        if (built.ExitCode != 0)
        {
            return (built, built);
        }

        // -getProperty only evaluates the project and builds nothing, so it must follow the build.
        var evaluated = await Task.Run(() => ProcessRunner.Run("dotnet", [.. arguments, "-getProperty:TargetPath"], repositoryRoot), cancellationToken);
        return (built, evaluated);
    }
}
