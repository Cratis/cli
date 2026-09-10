// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Generates a Cratis Screenplay (<c>.play</c>) file from the source code of a Cratis Arc application — reads the
/// solution or project with Roslyn, hands the compilation to the Screenplay generator, and writes the result.
/// </summary>
[LlmDescription("Generates a Cratis Screenplay (.play) file from Cratis Arc SOURCE CODE. Reads a solution or project with Roslyn — it never connects to a running application, so nothing needs to be started first. Writes to Screenplay.play in the current directory unless --file is given, and never overwrites an existing file of that name. Diagnostics for anything that could not be expressed go to standard error, grouped by severity; the command exits with a validation error when any of them is an error.")]
[CliCommand("generate", "Generate a Screenplay from Arc source code", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "generate")]
[CliExample("screenplay", "generate", "./MyApp.slnx", "--file", "MyApp.play")]
[CliExample("screenplay", "generate", "./Source/MyApp/MyApp.csproj")]
[LlmOption("[PATH]", "string", "Solution (.slnx, .sln), project (.csproj), or folder to read. Defaults to the current directory, searching upwards for a solution or project.")]
[LlmOption("--file", "string", "File to write the generated Screenplay to. Defaults to Screenplay.play in the current directory — or Screenplay-1.play, Screenplay-2.play, and so on when that already exists.")]
[LlmOption("--domain", "string", "Name of the domain the generated document belongs to.")]
[LlmOption("--module", "string", "Name of the module every discovered feature is placed within.")]
[LlmOption("--skip-segments", "int", "Number of leading namespace segments to skip when inferring features and slices.")]
[LlmOutputAdvice("json-compact", "JSON reports the written path and the module/feature/slice/command/event counts of the document as one object; diagnostics still go to standard error.")]
public class GenerateScreenplayCommand : AsyncCommand<GenerateScreenplaySettings>
{
    readonly IScreenplayGeneration _generation;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateScreenplayCommand"/> class.
    /// </summary>
    public GenerateScreenplayCommand()
        : this(ScreenplayGenerations.Create())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateScreenplayCommand"/> class.
    /// </summary>
    /// <param name="generation">The generation to produce the Screenplay with.</param>
    internal GenerateScreenplayCommand(IScreenplayGeneration generation)
    {
        _generation = generation;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, GenerateScreenplaySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var currentDirectory = Directory.GetCurrentDirectory();

        var target = ScreenplayTargetResolver.Resolve(settings.Path, currentDirectory);
        if (!target.IsResolved)
        {
            OutputFormatter.WriteError(format, target.Error!, target.Suggestion, ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var outputPath = string.IsNullOrWhiteSpace(settings.File)
            ? ScreenplayDocument.ResolveDefaultPath(currentDirectory)
            : ScreenplayDocument.ResolvePath(settings.File, currentDirectory);

        var started = Stopwatch.GetTimestamp();
        var generated = await GenerateWithProgress(format, target.Path!, outputPath, settings.ToGenerationOptions(), cancellationToken);
        var duration = Stopwatch.GetElapsedTime(started);

        ScreenplayDiagnosticsWriter.Write(format, generated.Diagnostics);

        var exitCode = ScreenplayDiagnostics.ExitCodeFor(generated.Diagnostics);
        if (exitCode != ExitCodes.Success)
        {
            OutputFormatter.WriteError(
                format,
                ErrorFor(generated),
                $"The document was still written to {outputPath} — review it, then resolve the reported errors",
                ExitCodes.ValidationErrorCode);
            return exitCode;
        }

        WriteResult(format, outputPath, target.Path!, generated, ScreenplayStatistics.For(generated.Source), duration);
        return ExitCodes.Success;
    }

    static string ErrorFor(GeneratedScreenplay generated) =>
        $"Screenplay generation reported {generated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error)} error(s)";

    static void WriteResult(string format, string outputPath, string targetPath, GeneratedScreenplay generated, ScreenplayStatistics statistics, TimeSpan duration)
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
                statistics.Modules,
                statistics.Features,
                statistics.Slices,
                statistics.Commands,
                statistics.Events,
                statistics.Queries,
                statistics.Reactors,
                statistics.Constraints,
                statistics.Concepts,
                statistics.Policies,
                Lines = CountLines(generated.Source),
                Diagnostics = generated.Diagnostics.Count,
                DurationMs = (long)duration.TotalMilliseconds
            },
            result =>
            {
                // Which projects took part is the difference between a document describing the whole application
                // and one describing part of it, so the panel says so rather than only naming what was read.
                var content = new Markup(
                    $"[bold]{result.Path.EscapeMarkup()}[/]\n" +
                    $"Source:      {result.Source.EscapeMarkup()}\n" +
                    $"Projects:    {string.Join(", ", result.Projects).EscapeMarkup()}\n" +
                    $"Modules:     {result.Modules}\n" +
                    $"Features:    {result.Features}\n" +
                    $"Slices:      {result.Slices}\n" +
                    $"Commands:    {result.Commands}\n" +
                    $"Events:      {result.Events}\n" +
                    $"Queries:     {result.Queries}\n" +
                    $"Reactors:    {result.Reactors}\n" +
                    $"Constraints: {result.Constraints}\n" +
                    $"Concepts:    {result.Concepts}\n" +
                    $"Policies:    {result.Policies}\n" +
                    $"Diagnostics: {result.Diagnostics}\n" +
                    $"Time:        {FormatDuration(duration)}");
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

    static string FormatDuration(TimeSpan duration) =>
        duration.TotalSeconds < 1
            ? $"{duration.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)}ms"
            : $"{duration.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)}s";

    /// <summary>
    /// Runs generation and the write to disk, showing a spinner with the step currently under way when the output
    /// is an interactive terminal.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="targetPath">The solution or project to generate from.</param>
    /// <param name="outputPath">The file to write the document to.</param>
    /// <param name="options">The options that shape the generated document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The <see cref="GeneratedScreenplay"/>.</returns>
    async Task<GeneratedScreenplay> GenerateWithProgress(string format, string targetPath, string outputPath, ScreenplayGenerationOptions options, CancellationToken cancellationToken)
    {
        if (!string.Equals(format, OutputFormats.Table, StringComparison.Ordinal))
        {
            return await GenerateAndWrite(targetPath, outputPath, options, _ => { }, cancellationToken);
        }

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(new Style(OutputFormatter.Accent))
            .StartAsync(
                "Generating the Screenplay document...",
                async ctx => await GenerateAndWrite(targetPath, outputPath, options, step => ctx.Status(step.EscapeMarkup()), cancellationToken));
    }

    async Task<GeneratedScreenplay> GenerateAndWrite(string targetPath, string outputPath, ScreenplayGenerationOptions options, Action<string> reportStep, CancellationToken cancellationToken)
    {
        var generated = await _generation.Generate(targetPath, options, reportStep, cancellationToken);
        reportStep($"Writing {Path.GetFileName(outputPath)}");
        await ScreenplayDocument.WriteToFile(outputPath, generated.Source, cancellationToken);
        return generated;
    }
}
