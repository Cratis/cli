// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Renders Screenplay documents into a Cratis application with the Stage renderer.
/// </summary>
/// <remarks>
/// This is the only place in the CLI that knows the renderer exists, the way
/// <see cref="ScreenplayValidation"/> is the only place that knows the compiler does.
/// </remarks>
/// <param name="validation">Compiles the documents and reports what the compiler found.</param>
/// <param name="renderer">Renders the compiled applications.</param>
public sealed class ScreenplayRendering(IScreenplayValidation validation, IRenderer renderer) : IScreenplayRendering
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayRendering"/> class with the default compiler and the
    /// Cratis renderer.
    /// </summary>
    public ScreenplayRendering()
        : this(new ScreenplayValidation(), CratisRenderer.CreateDefault())
    {
    }

    /// <inheritdoc/>
    public async Task<RenderedScreenplay> Render(string targetPath, string outputDirectory)
    {
        var compiled = validation.Validate(targetPath);
        var diagnostics = compiled.Diagnostics;

        // A document the compiler rejected has no application to render, and rendering the ones beside it would
        // produce an application missing whatever the rejected document declared - without saying which parts.
        if (diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error))
        {
            return new(compiled.FileCount, diagnostics, []);
        }

        var output = new StringWriter();
        var error = new StringWriter();

        await renderer.Render(compiled.Applications, new DirectoryInfo(outputDirectory), output, error);

        return new(compiled.FileCount, diagnostics, Lines(error));
    }

    static IReadOnlyList<string> Lines(StringWriter writer) =>
        [.. writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
