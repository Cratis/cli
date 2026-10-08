// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Screenplay documents with the <c language="csharp">Cratis.Screenplay</c> compiler.
/// </summary>
/// <remarks>
/// Diagnostics are translated into the same shape generation reports. A file is the root of an application,
/// including its imports; a folder compiles every document beneath it as one application.
/// </remarks>
public sealed class ScreenplayValidation : IScreenplayValidation
{
    readonly IPlayFileCompiler _playFileCompiler;
    readonly ScreenplayBinding _binding = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayValidation"/> class with the default compilers.
    /// </summary>
    public ScreenplayValidation()
        : this(new PlayFileCompiler(), new ScreenplayCompiler())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayValidation"/> class.
    /// </summary>
    /// <param name="playFileCompiler">Compiles an application from a root file or folder.</param>
    /// <param name="compiler">The single-document compiler, retained for constructor compatibility.</param>
#pragma warning disable IDE0290 // Keep the existing constructor signature without capturing the obsolete single-document compiler.
    public ScreenplayValidation(IPlayFileCompiler playFileCompiler, IScreenplayCompiler compiler)
    {
        _playFileCompiler = playFileCompiler;
    }
#pragma warning restore IDE0290

    /// <inheritdoc/>
    public ValidatedScreenplay Validate(string targetPath) => Validate(targetPath, CompletenessChecks.None);

    /// <inheritdoc/>
    public ValidatedScreenplay ValidateExecutable(string targetPath) => ValidateExecutable(targetPath, CompletenessChecks.None);

    /// <inheritdoc/>
    public ValidatedScreenplay Validate(string targetPath, CompletenessChecks checks)
    {
        var (compilation, sources) = Compile(targetPath);
        return Validated(compilation, sources, checks);
    }

    /// <inheritdoc/>
    public ValidatedScreenplay ValidateExecutable(string targetPath, CompletenessChecks checks)
    {
        var (compilation, sources) = Compile(targetPath);
        var validated = Validated(compilation, sources, checks);
        if (sources.Length == 0 || !compilation.Result.Success)
        {
            return validated with { Executable = false };
        }

        // Binding compiles the same documents again, so only what it adds beyond the source validation is reported.
        var root = File.Exists(targetPath) ? Path.GetDirectoryName(targetPath)! : targetPath;
        var (executable, diagnostics) = _binding.Bind(root, sources);
        var reported = validated.Diagnostics.ToHashSet();
        return validated with
        {
            Diagnostics = [.. validated.Diagnostics, .. diagnostics.Where(diagnostic => !reported.Contains(diagnostic))],
            Executable = executable
        };
    }

    static ValidatedScreenplay Validated(ApplicationCompilation<ApplicationSyntax> compilation, PlayFileSource[] sources, CompletenessChecks checks)
    {
        var result = compilation.Result;
        var errors = result.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var requested = checks.Selected.Count > 0;
        var findings = ModelCompleteness.Check(result, checks);
        return new(sources.Length, [.. result.Diagnostics.Concat(findings).Select(diagnostic => Map(diagnostic.Location.Path, diagnostic))])
        {
            Applications = sources.Length > 0 && result.Value is { } application ? [application] : [],
            Checks = checks,
            CompletenessStatus = (requested, result.Success) switch
            {
                (false, _) => "not requested",
                (_, true) => "ran",
                _ => "skipped"
            },
            CompletenessNote = requested && !result.Success ? $"completeness checks skipped: the model has {errors} error(s)" : null
        };
    }

    static ScreenplayDiagnostic Map(string? path, Diagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            path is null ? null : $"{path}({diagnostic.Location.Line},{diagnostic.Location.Column})");

    (ApplicationCompilation<ApplicationSyntax> Compilation, PlayFileSource[] Sources) Compile(string targetPath)
    {
        var compilation = File.Exists(targetPath)
            ? _playFileCompiler.CompileApplication(targetPath)
            : _playFileCompiler.CompileFolder(targetPath);
        return (compilation, compilation.Sources.ToArray());
    }
}
