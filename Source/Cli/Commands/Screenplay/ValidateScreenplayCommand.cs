// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Cratis Screenplay (<c>.play</c>) documents and reports everything the compiler found, whatever wrote
/// them — <c>screenplay generate</c>, <c>prologue</c>, or a person.
/// </summary>
[LlmDescription("Compiles Cratis Screenplay (.play) documents and reports every diagnostic the compiler produces. Takes a .play file, or a folder in which case every .play file beneath it is compiled. Nothing needs to be running. Diagnostics go to standard error, grouped by severity; the command exits with a validation error when any of them is an error.")]
[CliCommand("validate", "Validate Screenplay (.play) documents", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "validate")]
[CliExample("screenplay", "validate", "./MyApp.play")]
[CliExample("screenplay", "validate", "./plays")]
[LlmOption("[PATH]", "string", "Screenplay (.play) file, or folder to compile every .play file beneath. Defaults to the current directory.")]
[LlmOutputAdvice("json-compact", "The summary goes to standard output and the diagnostics to standard error; json-compact makes both machine-readable.")]
public class ValidateScreenplayCommand : Command<ValidateScreenplaySettings>
{
    readonly IScreenplayValidation _validation;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateScreenplayCommand"/> class.
    /// </summary>
    public ValidateScreenplayCommand()
        : this(new ScreenplayValidation())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateScreenplayCommand"/> class.
    /// </summary>
    /// <param name="validation">The validation to compile the documents with.</param>
    internal ValidateScreenplayCommand(IScreenplayValidation validation)
    {
        _validation = validation;
    }

    /// <inheritdoc/>
    protected override int Execute(CommandContext context, ValidateScreenplaySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        var target = PlayFileTargetResolver.Resolve(settings.Path, Directory.GetCurrentDirectory());
        if (!target.IsResolved)
        {
            OutputFormatter.WriteError(format, target.Error!, target.Suggestion, ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var validated = _validation.Validate(target.Path!);
        if (validated.FileCount == 0)
        {
            // Silently succeeding on a folder holding nothing turns the command into a no-op in CI, which is
            // exactly where it is trusted the most.
            OutputFormatter.WriteError(
                format,
                $"No Screenplay ({PlayFileTargetResolver.Extension}) files found in '{target.Path}'",
                $"Point the command at a {PlayFileTargetResolver.Extension} file, or at a folder holding one",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        ScreenplayDiagnosticsWriter.Write(format, validated.Diagnostics);

        var exitCode = ScreenplayDiagnostics.ExitCodeFor(validated.Diagnostics);
        if (exitCode != ExitCodes.Success)
        {
            var errors = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
            OutputFormatter.WriteError(
                format,
                $"Validation reported {errors} error(s)",
                "Fix the reported errors in the Screenplay document",
                ExitCodes.ValidationErrorCode);
            return exitCode;
        }

        WriteResult(format, target.Path!, validated);
        return ExitCodes.Success;
    }

    static void WriteResult(string format, string targetPath, ValidatedScreenplay validated)
    {
        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
        {
            Console.WriteLine(targetPath);
            return;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                Path = targetPath,
                Files = validated.FileCount,
                Diagnostics = validated.Diagnostics.Count
            },
            result =>
            {
                var content = new Markup(
                    $"[bold]{result.Path.EscapeMarkup()}[/]\n" +
                    $"Files:       {result.Files}\n" +
                    $"Diagnostics: {result.Diagnostics}");
                var panel = new Panel(content)
                    .Header(" Valid ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(OutputFormatter.Success))
                    .Padding(1, 0);

                AnsiConsole.WriteLine();
                AnsiConsole.Write(panel);
            });
    }
}
