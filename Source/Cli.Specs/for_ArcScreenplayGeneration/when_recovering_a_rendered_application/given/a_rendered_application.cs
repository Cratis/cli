// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_recovering_a_rendered_application.given;

/// <summary>
/// Renders a canonical corpus vector with the real planning and publication, restores the rendered application inside a
/// clean git root, and recovers it with the real Arc generation - the render-then-recover round trip (Screenplay#168.11).
/// </summary>
[Collection(CliSpecsCollection.Name)]
public abstract class a_rendered_application : Specification
{
    protected string _root = null!;
    protected string _source = null!;
    protected string _rendered = null!;
    private protected GeneratedScreenplay _recovered = null!;

    /// <summary>
    /// Gets the application name the corpus vector renders as.
    /// </summary>
    protected abstract string ApplicationName { get; }

    /// <summary>
    /// Gets the documents of the vector's folder source form.
    /// </summary>
    protected abstract IEnumerable<CanonicalCorpusDocument> Documents { get; }

    /// <summary>
    /// Gets the declarations the source declares.
    /// </summary>
    protected IReadOnlySet<string> SourceDeclarations => PlayDeclarations.In(string.Join('\n', Directory
        .EnumerateFiles(_source, "*.play", SearchOption.AllDirectories)
        .Order(StringComparer.Ordinal)
        .Select(File.ReadAllText)));

    /// <summary>
    /// Gets the declarations the recovered document declares.
    /// </summary>
    protected IReadOnlySet<string> RecoveredDeclarations => PlayDeclarations.In(_recovered.Source);

    /// <summary>
    /// Renders the vector, restores the rendered application inside a clean git root and recovers it.
    /// </summary>
    /// <returns>A task that completes when the application has been recovered.</returns>
    protected async Task RenderAndRecover()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-recover-{Guid.NewGuid():N}")).FullName;
        _source = Path.Combine(_root, "source");
        _rendered = Path.Combine(_root, "rendered");
        foreach (var document in Documents)
        {
            var path = Path.Combine(_source, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }

        var plan = await new ScreenplayPlanning().Plan(new(_source, ApplicationName, CratisRendering.TargetId, null, null), CancellationToken.None);
        plan.Success.ShouldBeTrue();
        foreach (var artifact in plan.Artifacts!.Artifacts)
        {
            var path = Path.Combine(_rendered, artifact.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, artifact.Bytes.AsMemory());
        }

        await Run(_rendered, "git", "init", "--quiet");
        await Run(_rendered, "dotnet", "restore");

        var project = Directory.EnumerateFiles(_rendered, "*.csproj").Single();
        _recovered = await new ArcScreenplayGeneration().Generate(project, ScreenplayGenerationOptions.Default, _ => { }, CancellationToken.None);
    }

    void Destroy()
    {
        if (!string.IsNullOrEmpty(_root) && Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    static async Task Run(string workingDirectory, string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new RenderedApplicationFixtureFailed($"'{fileName}' could not be started");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new RenderedApplicationFixtureFailed($"'{fileName} {string.Join(' ', arguments)}' failed: {await output}{await error}");
        }
    }
}

sealed class RenderedApplicationFixtureFailed(string message) : Exception(message);
