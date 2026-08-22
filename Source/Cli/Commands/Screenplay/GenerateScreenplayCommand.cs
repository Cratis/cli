// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Generates a Cratis Screenplay (<c>.play</c>) file from Arc, Marten, or Critter Stack application source — reads
/// the solution or project with Roslyn, hands the compilation to the selected generator, and writes the result.
/// </summary>
[LlmDescription("Generates a Cratis Screenplay (.play) file from Arc, Marten, or Critter Stack SOURCE CODE. Reads a solution or project with Roslyn — it never connects to a running application, so nothing needs to be started first. Writes the .play source to standard output unless --file is given. Diagnostics for anything that could not be expressed go to standard error, grouped by severity; the command exits with a validation error when any of them is an error.")]
[CliCommand("generate", "Generate a Screenplay from application source code", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "generate")]
[CliExample("screenplay", "generate", "./MyApp.slnx", "--file", "MyApp.play")]
[CliExample("screenplay", "generate", "./Source/MyApp/MyApp.csproj")]
[CliExample("screenplay", "generate", "--modules-from-namespace-roots", "--skip-segments", "1")]
[LlmOption("[PATH]", "string", "Solution (.slnx, .sln, .slnf), project (.csproj), or folder to read. Defaults to the current directory, searching upwards for a solution or project.")]
[LlmOption("--file", "string", "File to write the generated Screenplay to. Writes to standard output when not given.")]
[LlmOption("--provider", "string", "Source framework provider: auto, arc, marten, or critter-stack.")]
[LlmOption("--framework", "string", "Target framework to load from multi-targeted projects. Required when any application project targets several frameworks.")]
[LlmOption("--domain", "string", "Name of the domain the generated document belongs to.")]
[LlmOption("--module", "string", "Name of the module every discovered feature is placed within.")]
[LlmOption("--skip-segments", "int", "Number of leading namespace segments to skip when inferring features and slices.")]
[LlmOption("--modules-from-namespace-roots", "bool", "Name the module of each feature after the outermost segment of its namespace, instead of placing every feature in one module. Combine with --skip-segments when every slice shares a root namespace.")]
[LlmOutputAdvice("json-compact", "The .play document always goes to standard output verbatim; the format only shapes the summary and the diagnostics, and json-compact makes the diagnostics machine-readable on standard error.")]
public class GenerateScreenplayCommand : AsyncCommand<GenerateScreenplaySettings>
{
    readonly IScreenplayGeneration _generation;
    readonly Func<Stream> _standardOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateScreenplayCommand"/> class.
    /// </summary>
    public GenerateScreenplayCommand()
        : this(ScreenplayGenerations.Create(), Console.OpenStandardOutput)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateScreenplayCommand"/> class.
    /// </summary>
    /// <param name="generation">The generation to produce the Screenplay with.</param>
    /// <param name="standardOutput">Opens the stream the document is written to when no file is given.</param>
    internal GenerateScreenplayCommand(IScreenplayGeneration generation, Func<Stream> standardOutput)
    {
        _generation = generation;
        _standardOutput = standardOutput;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, GenerateScreenplaySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var currentDirectory = Directory.GetCurrentDirectory();

        // Standard output carries the document itself unless a file is given, so failures may not be reported
        // through the ordinary panel — it would end up inside the redirected '.play' file.
        var writesDocumentToStandardOutput = string.IsNullOrWhiteSpace(settings.File);

        var target = ScreenplayTargetResolver.Resolve(settings.Path, currentDirectory);
        if (!target.IsResolved)
        {
            WriteError(format, writesDocumentToStandardOutput, target.Error!, target.Suggestion, ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var generated = await _generation.Generate(target.Path!, settings.ToGenerationOptions(), cancellationToken);
        ScreenplayDiagnosticsWriter.Write(format, generated.Diagnostics, generated.Provenance);

        var exitCode = ScreenplayDiagnostics.ExitCodeFor(generated.Diagnostics);

        // Standard output is the document itself, so a partial one cannot be written there — whatever consumes the
        // redirect would take it for a complete document. A file can, and is: the diagnostics say what is missing.
        if (writesDocumentToStandardOutput)
        {
            if (exitCode != ExitCodes.Success)
            {
                if (!ScreenplayDiagnosticsWriter.IsMachineReadable(format))
                {
                    WriteError(
                        format,
                        true,
                        ErrorFor(generated),
                        "Pass --file to write the document that was generated anyway, or resolve the reported errors",
                        ExitCodes.ValidationErrorCode);
                }

                return exitCode;
            }

            await using var stream = _standardOutput();
            await ScreenplayDocument.Write(stream, generated.Source, cancellationToken);
            return ExitCodes.Success;
        }

        var outputPath = ScreenplayDocument.ResolvePath(settings.File!, currentDirectory);
        await ScreenplayDocument.WriteToFile(outputPath, generated.Source, cancellationToken);

        if (exitCode != ExitCodes.Success)
        {
            if (!ScreenplayDiagnosticsWriter.IsMachineReadable(format))
            {
                WriteError(
                    format,
                    false,
                    ErrorFor(generated),
                    $"The document was still written to {outputPath} — review it, then resolve the reported errors",
                    ExitCodes.ValidationErrorCode);
            }

            return exitCode;
        }

        WriteResult(format, outputPath, target.Path!, generated);
        return ExitCodes.Success;
    }

    static string ErrorFor(GeneratedScreenplay generated) =>
        $"Screenplay generation reported {generated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error)} error(s)";

    static void WriteError(string format, bool keepStandardOutputClean, string error, string? suggestion, string errorCode)
    {
        if (!keepStandardOutputClean || GoesToStandardError(format))
        {
            OutputFormatter.WriteError(format, error, suggestion, errorCode);
            return;
        }

        Console.Error.WriteLine();
        Console.Error.WriteLine($"error: {error}");
        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            Console.Error.WriteLine($"  -> {suggestion}");
        }
    }

    /// <summary>
    /// Mirrors the formats <see cref="OutputFormatter.WriteError"/> already reports on standard error.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <returns><see langword="true"/> when the formatter writes errors to standard error.</returns>
    static bool GoesToStandardError(string format) =>
        string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal);

    static void WriteResult(string format, string outputPath, string targetPath, GeneratedScreenplay generated)
    {
        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
        {
            Console.WriteLine(outputPath);
            return;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                Path = outputPath,
                Source = targetPath,
                generated.Projects,
                Lines = CountLines(generated.Source),
                Diagnostics = generated.Diagnostics.Count
            },
            result =>
            {
                // Which projects took part is the difference between a document describing the whole application
                // and one describing part of it, so the panel says so rather than only naming what was read.
                var content = new Markup(
                    $"[bold]{result.Path.EscapeMarkup()}[/]\n" +
                    $"Source:      {result.Source.EscapeMarkup()}\n" +
                    $"Projects:    {string.Join(", ", result.Projects).EscapeMarkup()}\n" +
                    $"Lines:       {result.Lines}\n" +
                    $"Diagnostics: {result.Diagnostics}");
                var panel = new Panel(content)
                    .Header(" Screenplay generated ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(OutputFormatter.Success))
                    .Padding(1, 0);

                AnsiConsole.WriteLine();
                AnsiConsole.Write(panel);
                AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]→ Run it in a local Stage sandbox with: cratis run[/]");
            });
    }

    static int CountLines(string source) =>
        source.Length == 0 ? 0 : source.AsSpan().TrimEnd('\n').Count('\n') + 1;
}
