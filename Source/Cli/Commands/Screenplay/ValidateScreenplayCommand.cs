// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Cratis Screenplay (<c language="csharp">.play</c>) documents and reports everything the compiler found, whatever wrote
/// them — <c language="csharp">screenplay generate</c>, <c language="csharp">prologue</c>, or a person.
/// </summary>
[LlmDescription("Compiles Cratis Screenplay (.play) documents and reports every diagnostic the compiler produces. Takes a root .play file together with its imports, or a folder in which case every .play file beneath it is compiled as one application. Nothing needs to be running. Diagnostics go to standard error, grouped by severity; the command exits with a validation error when any of them is an error.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("validate", "Validate Screenplay (.play) documents", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "validate")]
[CliExample("screenplay", "validate", "./MyApp.play")]
[CliExample("screenplay", "validate", "./plays")]
[CliExample("screenplay", "validate", "--executable", "./plays")]
[LlmOption("[PATH]", "string", "Root Screenplay (.play) file with its imports, or folder to compile every .play file beneath as one application. Defaults to the current directory.")]
[LlmOption("--warnings-as-errors", "boolean", "Treat compiler warnings as validation errors.")]
[LlmOption("--executable", "boolean", "Also bind the model into an executable semantic model; fails with binding diagnostics such as PLAY0268 when it does not bind. Never renders or writes files. A pass without this option only means the source is valid.")]
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
    public override int Execute(CommandContext context, ValidateScreenplaySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        var target = PlayFileTargetResolver.Resolve(settings.Path, Directory.GetCurrentDirectory());
        if (!target.IsResolved)
        {
            OutputFormatter.WriteError(format, target.Error!, target.Suggestion, ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var validated = settings.Executable ? _validation.ValidateExecutable(target.Path!) : _validation.Validate(target.Path!);
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

        var exitCode = validated.Executable == false
            ? ExitCodes.ValidationError
            : ScreenplayDiagnostics.ExitCodeFor(validated.Diagnostics, settings.WarningsAsErrors);
        if (exitCode != ExitCodes.Success)
        {
            var errors = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
            var warnings = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning);
            var message = settings.WarningsAsErrors
                ? $"Validation reported {errors} error(s) and {warnings} warning(s)"
                : $"Validation reported {errors} error(s)";
            if (validated.Executable == false && errors == 0)
            {
                message = "The model does not bind into an executable semantic model";
            }

            var suggestion = settings.WarningsAsErrors
                ? "Fix the reported errors and warnings in the Screenplay document"
                : "Fix the reported errors in the Screenplay document";
            OutputFormatter.WriteError(format, message, suggestion, ExitCodes.ValidationErrorCode);
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
                Diagnostics = validated.Diagnostics.Count,
                Checked = validated.Executable is null ? "source" : "executable",
                validated.Executable
            },
            result =>
            {
                var content = new Markup(
                    $"[bold]{result.Path.EscapeMarkup()}[/]\n" +
                    $"Files:       {result.Files}\n" +
                    $"Diagnostics: {result.Diagnostics}\n" +
                    $"Checked:     {(result.Executable is null ? "source only; use --executable to check that the model binds" : "source and executable binding")}");
                var panel = new Panel(content)
                    .Header(result.Executable is null ? " Valid " : " Valid and executable ")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(OutputFormatter.Success))
                    .Padding(1, 0);

                AnsiConsole.WriteLine();
                AnsiConsole.Write(panel);
            });
    }
}
