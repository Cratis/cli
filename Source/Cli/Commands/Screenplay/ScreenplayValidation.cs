// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

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
    public ScreenplayValidation(IPlayFileCompiler playFileCompiler, IScreenplayCompiler compiler)
    {
        _playFileCompiler = playFileCompiler;
    }

    /// <inheritdoc/>
    public ValidatedScreenplay Validate(string targetPath)
    {
        var compilation = File.Exists(targetPath)
            ? _playFileCompiler.CompileApplication(targetPath)
            : _playFileCompiler.CompileFolder(targetPath);
        var sources = compilation.Sources.ToArray();

        return new(sources.Length, [.. compilation.Result.Diagnostics.Select(diagnostic => Map(diagnostic.Location.Path, diagnostic))])
        {
            Applications = sources.Length > 0 && compilation.Result.Value is { } application ? [application] : []
        };
    }

    static ScreenplayDiagnostic Map(string? path, Diagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            path is null ? null : $"{path}({diagnostic.Location.Line},{diagnostic.Location.Column})");
}
