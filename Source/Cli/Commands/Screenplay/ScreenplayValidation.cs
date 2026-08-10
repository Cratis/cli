// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Screenplay documents with the <c>Cratis.Screenplay</c> compiler.
/// </summary>
/// <remarks>
/// This is the only place in the CLI that knows the compiler exists. Everything else is expressed against
/// <see cref="IScreenplayValidation"/>, and the diagnostics the compiler reports are translated into the same shape
/// generation reports, so both commands read identically.
/// </remarks>
/// <param name="playFileCompiler">Compiles every document beneath a folder.</param>
/// <param name="compiler">Compiles the source of a single document.</param>
public sealed class ScreenplayValidation(IPlayFileCompiler playFileCompiler, IScreenplayCompiler compiler) : IScreenplayValidation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayValidation"/> class with the default compilers.
    /// </summary>
    public ScreenplayValidation()
        : this(new PlayFileCompiler(), new ScreenplayCompiler())
    {
    }

    /// <inheritdoc/>
    public ValidatedScreenplay Validate(string targetPath)
    {
        var compilations = File.Exists(targetPath)
            ? [CompileFile(targetPath)]
            : playFileCompiler.CompileIn(targetPath).ToArray();

        return new(
            compilations.Length,
            [.. compilations.SelectMany(compilation => compilation.Result.Diagnostics.Select(diagnostic => Map(compilation.File, diagnostic)))]);
    }

    /// <summary>
    /// Translates a compiler diagnostic into the shape the CLI reports.
    /// </summary>
    /// <param name="file">The file the diagnostic belongs to.</param>
    /// <param name="diagnostic">The diagnostic the compiler reported.</param>
    /// <returns>The <see cref="ScreenplayDiagnostic"/>.</returns>
    /// <remarks>
    /// The compiler assigns every diagnostic a stable <c>PLAY</c> code, which is carried through so that a
    /// diagnostic can be looked up, suppressed or matched on rather than only read. The location carries the file
    /// and the position within it, in the <c>file(line,column)</c> form editors and build logs already understand.
    /// </remarks>
    static ScreenplayDiagnostic Map(PlayFile file, Diagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            $"{file.RelativePath}({diagnostic.Location.Line},{diagnostic.Location.Column})");

    PlayFileCompilation CompileFile(string path)
    {
        var source = File.ReadAllText(path);
        return new(new PlayFile(path, Path.GetFileName(path)), source, compiler.Compile(source));
    }
}
