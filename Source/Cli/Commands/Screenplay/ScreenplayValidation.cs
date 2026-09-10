// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Screenplay documents with the <c>Cratis.Screenplay</c> compiler.
/// </summary>
/// <remarks>
/// Validation compiles a folder as one application, independently of renderer admission. Compiler diagnostics
/// are translated into the same shape generation reports, so both commands read identically.
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
        if (File.Exists(targetPath))
        {
            var file = CompileFile(targetPath);
            return Map(file.Result, 1, file.File.RelativePath);
        }

        var folder = playFileCompiler.CompileFolder(targetPath);
        return Map(folder.Result, folder.Sources.Count(), "Screenplay");
    }

    /// <summary>
    /// Translates a compiler diagnostic into the shape the CLI reports.
    /// </summary>
    /// <param name="fallbackPath">The display path for a diagnostic without a source path.</param>
    /// <param name="diagnostic">The diagnostic the compiler reported.</param>
    /// <returns>The <see cref="ScreenplayDiagnostic"/>.</returns>
    /// <remarks>
    /// The compiler assigns every diagnostic a stable <c>PLAY</c> code, which is carried through so that a
    /// diagnostic can be looked up, suppressed or matched on rather than only read. The location carries the file
    /// and the position within it, in the <c>file(line,column)</c> form editors and build logs already understand.
    /// </remarks>
    static ScreenplayDiagnostic Map(string fallbackPath, Diagnostic diagnostic) =>
        new(
            (ScreenplayDiagnosticSeverity)(int)diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            $"{diagnostic.Location.Path ?? fallbackPath}({diagnostic.Location.Line},{diagnostic.Location.Column})");

    static ValidatedScreenplay Map(CompilationResult<ApplicationSyntax> result, int fileCount, string fallbackPath) =>
        new(fileCount, [.. result.Diagnostics.Select(diagnostic => Map(fallbackPath, diagnostic))])
        {
            Applications = fileCount > 0 && result.Value is { } application ? [application] : []
        };

    PlayFileCompilation CompileFile(string path)
    {
        var source = File.ReadAllText(path);
        return new(new PlayFile(path, Path.GetFileName(path)), source, compiler.Compile(source));
    }
}
