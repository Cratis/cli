// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Renders Screenplay (<c>.play</c>) documents into a working Cratis application.
/// </summary>
/// <remarks>
/// A sibling of <c>run</c> rather than a mode of it: <c>run</c> hands a folder to a container and watches, while
/// this materializes the application as source on disk and exits. They share only how a document is found.
/// </remarks>
[LlmDescription("Renders Cratis Screenplay (.play) documents into a Cratis application on disk. Takes a .play file, or a folder in which case every .play file beneath it is rendered into one application. Nothing needs to be running. The target project is scaffolded on first use. What the document states but the rendered application cannot express is reported to standard error rather than dropped silently; the command still succeeds. A document the compiler rejects is not rendered at all.")]
[CliCommand("render", "Render Screenplay (.play) documents into a Cratis application")]
[CliExample("render")]
[CliExample("render", "./MyApp.play")]
[CliExample("render", "./plays", "--target", "./src/MyApp")]
[LlmOption("[PATH]", "string", "Screenplay (.play) file, or folder to render every .play file beneath. Defaults to the current directory.")]
[LlmOption("--target", "string", "Directory to render the application into (default: ./out).")]
[LlmOutputAdvice("json-compact", "The summary goes to standard output and what could not be rendered to standard error; json-compact makes both machine-readable.")]
public class RenderCommand : AsyncCommand<RenderSettings>
{
    /// <summary>
    /// The directory rendered into when none is given.
    /// </summary>
    public const string DefaultTarget = "out";

    readonly IScreenplayRendering _rendering;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderCommand"/> class.
    /// </summary>
    public RenderCommand()
        : this(new ScreenplayRendering())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderCommand"/> class.
    /// </summary>
    /// <param name="rendering">The rendering to render the documents with.</param>
    internal RenderCommand(IScreenplayRendering rendering)
    {
        _rendering = rendering;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, RenderSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var currentDirectory = Directory.GetCurrentDirectory();

        var resolved = PlayFileTargetResolver.Resolve(settings.Path, currentDirectory);
        if (!resolved.IsResolved)
        {
            OutputFormatter.WriteError(format, resolved.Error!, resolved.Suggestion, ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var target = Path.GetFullPath(settings.Target ?? DefaultTarget, currentDirectory);
        var rendered = await _rendering.Render(resolved.Path!, target);

        if (rendered.Documents == 0)
        {
            // Silently succeeding on a folder holding nothing turns the command into a no-op in CI, which is
            // exactly where it is trusted the most.
            OutputFormatter.WriteError(
                format,
                $"No Screenplay ({PlayFileTargetResolver.Extension}) files found in '{resolved.Path}'",
                $"Point the command at a {PlayFileTargetResolver.Extension} file, or at a folder holding one",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        ScreenplayDiagnosticsWriter.Write(format, rendered.Diagnostics);

        var exitCode = ScreenplayDiagnostics.ExitCodeFor(rendered.Diagnostics);
        if (exitCode != ExitCodes.Success)
        {
            var errors = rendered.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
            OutputFormatter.WriteError(
                format,
                $"Nothing was rendered — the document reported {errors} error(s)",
                "Fix the reported errors in the Screenplay document, then render again",
                ExitCodes.ValidationErrorCode);
            return exitCode;
        }

        WriteReported(rendered);
        WriteResult(format, target, rendered);
        return ExitCodes.Success;
    }

    /// <summary>
    /// Writes what the rendered application does not carry. This is not a failure — a Screenplay document states
    /// more than any one target expresses — but it is the difference between the document and what was produced,
    /// so it goes to standard error where it will be read rather than into a summary line.
    /// </summary>
    /// <param name="rendered">What rendering produced.</param>
    static void WriteReported(RenderedScreenplay rendered)
    {
        foreach (var reported in rendered.Reported)
        {
            Console.Error.WriteLine(reported);
        }
    }

    static void WriteResult(string format, string targetDirectory, RenderedScreenplay rendered)
    {
        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
        {
            Console.WriteLine(targetDirectory);
            return;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                Target = targetDirectory,
                rendered.Documents,
                NotCarried = rendered.Reported.Count
            },
            result =>
            {
                var content = new Markup(
                    $"[bold]{result.Target.EscapeMarkup()}[/]\n" +
                    $"Documents:   {result.Documents}\n" +
                    $"Not carried: {result.NotCarried}");
                var panel = new Panel(content)
                    .Header(" Rendered ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(OutputFormatter.Success))
                    .Padding(1, 0);

                AnsiConsole.WriteLine();
                AnsiConsole.Write(panel);
            });
    }
}
