// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Resolves what a project builds by asking MSBuild to evaluate it.
/// </summary>
/// <remarks>
/// The output path of a project is whatever its evaluation says - an <c language="msbuild">OutputPath</c>, an artifacts layout, a
/// <c language="msbuild">Directory.Build.props</c> several folders up - so it is read from MSBuild rather than reconstructed from
/// conventions. Only evaluation runs; nothing is restored or built.
/// </remarks>
public static class ProjectOutputResolver
{
    static readonly string[] _properties = ["AssemblyName", "RootNamespace", "TargetFramework", "TargetFrameworks", "TargetPath"];

    /// <summary>
    /// Resolves what a project builds.
    /// </summary>
    /// <param name="projectFile">The full path of the project file.</param>
    /// <param name="configuration">The build configuration to resolve the output of.</param>
    /// <param name="framework">The target framework to resolve, or <see langword="null"/> for the first the project lists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="ProjectOutput"/>.</returns>
    /// <exception cref="ProjectCouldNotBeEvaluated">Thrown when MSBuild cannot evaluate the project.</exception>
    public static async Task<ProjectOutput> Resolve(string projectFile, string configuration, string? framework, CancellationToken cancellationToken)
    {
        var properties = await Evaluate(projectFile, configuration, framework, cancellationToken);

        // A project targeting several frameworks has no single output until one of them is chosen, so it is
        // evaluated a second time for the first one it lists.
        if (string.IsNullOrWhiteSpace(framework) && string.IsNullOrWhiteSpace(properties.GetValueOrDefault("TargetPath")))
        {
            var first = properties.GetValueOrDefault("TargetFrameworks")?
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (first is not null)
            {
                properties = await Evaluate(projectFile, configuration, first, cancellationToken);
            }
        }

        var assemblyName = Value(properties, "AssemblyName") ?? Path.GetFileNameWithoutExtension(projectFile);
        var targetPath = Value(properties, "TargetPath") ??
            throw new ProjectCouldNotBeEvaluated(projectFile, "MSBuild did not state where the project's output assembly is built");

        return new ProjectOutput(
            assemblyName,
            Value(properties, "RootNamespace") ?? assemblyName,
            Value(properties, "TargetFramework") ?? framework ?? string.Empty,
            targetPath);
    }

    static string? Value(IReadOnlyDictionary<string, string> properties, string name) =>
        properties.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    static async Task<IReadOnlyDictionary<string, string>> Evaluate(string projectFile, string configuration, string? framework, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(projectFile)
        };
        start.ArgumentList.Add("msbuild");
        start.ArgumentList.Add(projectFile);
        start.ArgumentList.Add("-nologo");
        start.ArgumentList.Add($"-p:Configuration={configuration}");
        if (!string.IsNullOrWhiteSpace(framework))
        {
            start.ArgumentList.Add($"-p:TargetFramework={framework}");
        }

        foreach (var property in _properties)
        {
            start.ArgumentList.Add($"-getProperty:{property}");
        }

        using var process = Process.Start(start) ??
            throw new ProjectCouldNotBeEvaluated(projectFile, "the 'dotnet' command could not be started");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var reason = FirstLine(await output) ?? FirstLine(await error) ?? $"MSBuild exited with {process.ExitCode}";
            throw new ProjectCouldNotBeEvaluated(projectFile, reason);
        }

        return Parse(projectFile, await output);
    }

    static Dictionary<string, string> Parse(string projectFile, string output)
    {
        try
        {
            using var document = JsonDocument.Parse(output);
            if (!document.RootElement.TryGetProperty("Properties", out var properties))
            {
                throw new ProjectCouldNotBeEvaluated(projectFile, "MSBuild answered without the properties asked for");
            }

            return properties
                .EnumerateObject()
                .ToDictionary(_ => _.Name, _ => _.Value.GetString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException ex)
        {
            throw new ProjectCouldNotBeEvaluated(projectFile, $"MSBuild answered with something that is not JSON: {ex.Message}");
        }
    }

    static string? FirstLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
}
