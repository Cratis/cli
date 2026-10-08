// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Cratis Screenplay (<c language="csharp">.play</c>) documents and reports everything the compiler found, whatever wrote
/// them — <c language="csharp">screenplay generate</c>, <c language="csharp">prologue</c>, or a person.
/// </summary>
[LlmDescription("Compiles Cratis Screenplay (.play) documents and reports every diagnostic the compiler produces. Takes a root .play file together with its imports, or a folder in which case every .play file beneath it is compiled as one application. Nothing needs to be running. Use repeatable --check selections for opt-in structural completeness warnings; these are skipped when source compilation has errors and do not prove runtime completeness. Completeness and --executable can be checked together. --scope selects source diagnostics for a module, feature or slice and its direct dependents; the exit code covers only that set and whole-application counts are always reported separately. Check executable binding for the whole application separately. Diagnostics go to standard error, grouped by severity; errors fail validation, while warnings fail only with --warnings-as-errors.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("validate", "Validate Screenplay (.play) documents", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "validate")]
[CliExample("screenplay", "validate", "./MyApp.play")]
[CliExample("screenplay", "validate", "./plays")]
[CliExample("screenplay", "validate", "--executable", "./plays")]
[CliExample("screenplay", "validate", "./plays", "--check", "all")]
[CliExample("screenplay", "validate", "./plays", "--check", "navigation", "--check", "field-origins", "--executable")]
[LlmOption("[PATH]", "string", "Root Screenplay (.play) file with its imports, or folder to compile every .play file beneath as one application. Defaults to the current directory.")]
[LlmOption("--warnings-as-errors", "boolean", "Treat compiler and completeness warnings as validation errors.")]
[LlmOption("--scope", "string", "Case-sensitive Module[.Feature[.Slice]] address. Selects source diagnostics and direct dependents; whole-application counts remain visible. Unknown or ambiguous scopes are usage errors. Combine with --check, not --executable.")]
[LlmOption("--check", "string[]", "Repeatable; selections union. Accepts comma-separated data-bindings (PLAY0530, PLAY0531), input-surfaces (PLAY0532, PLAY0533), field-origins (PLAY0534), query-keys (PLAY0535), event-consumers (PLAY0536), navigation (PLAY0537), or all. Skipped on source compilation errors. Findings are structural warnings, not proof of runtime completeness.")]
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

        var checks = settings.SelectedChecks;
        ValidatedScreenplay validated;
        if (settings.Scope is { } scope)
        {
            if (!_validation.TryValidateScoped(target.Path!, scope, checks, out var scoped, out var error))
            {
                OutputFormatter.WriteError(format, error!, "Select an existing module, feature or slice address in the application", ExitCodes.NotFoundCode);
                return ExitCodes.NotFound;
            }
            validated = scoped!;
        }
        else
        {
            validated = (checks.Selected.Count > 0, settings.Executable) switch
            {
                (false, false) => _validation.Validate(target.Path!),
                (false, true) => _validation.ValidateExecutable(target.Path!),
                (true, false) => _validation.Validate(target.Path!, checks),
                (true, true) => _validation.ValidateExecutable(target.Path!, checks)
            };
        }
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
            if (checks.Selected.Count > 0 || validated.Scoped is not null)
            {
                WriteResult(format, target.Path!, validated, false, settings.WarningsAsErrors);
            }
            return exitCode;
        }

        WriteResult(format, target.Path!, validated, warningsAsErrors: settings.WarningsAsErrors);
        return ExitCodes.Success;
    }

    static string CheckName(CompletenessCheck check) => check switch
    {
        CompletenessCheck.DataBindings => "data-bindings",
        CompletenessCheck.InputSurfaces => "input-surfaces",
        CompletenessCheck.FieldOrigins => "field-origins",
        CompletenessCheck.QueryKeys => "query-keys",
        CompletenessCheck.EventConsumers => "event-consumers",
        CompletenessCheck.Navigation => "navigation",
        _ => check.ToString()
    };

    static void WriteResult(string format, string targetPath, ValidatedScreenplay validated, bool success = true, bool warningsAsErrors = false)
    {
        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
        {
            if (success)
            {
                Console.WriteLine(targetPath);
            }
            if (validated.Scoped is { } scope)
            {
                Console.WriteLine($"Whole application: {scope.WholeApplicationErrorCount} error(s), {scope.WholeApplicationWarningCount} warning(s)");
            }
            return;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                Path = targetPath,
                Valid = success,
                Files = validated.FileCount,
                Diagnostics = validated.Diagnostics.Count,
                Errors = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error),
                Warnings = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning),
                Checked = validated.Executable is null ? "source" : "executable",
                validated.Executable,
                Checks = validated.Checks.Selected.Select(CheckName).Order(StringComparer.Ordinal).ToArray(),
                validated.CompletenessStatus,
                validated.CompletenessNote,
                validated.Scoped?.Scope,
                validated.Scoped?.DeclarationCount,
                validated.Scoped?.DependentDeclarationCount,
                validated.Scoped?.AffectedScopes,
                validated.Scoped?.UnresolvedEventConsumers,
                validated.Scoped?.PossiblyAffectedReferenceCount,
                validated.Scoped?.DependencyCoverage,
                WholeApplication = validated.Scoped is { } scoped ? new
                {
                    Valid = scoped.WholeApplicationErrorCount == 0 && (!warningsAsErrors || scoped.WholeApplicationWarningCount == 0),
                    Errors = scoped.WholeApplicationErrorCount,
                    Warnings = scoped.WholeApplicationWarningCount
                } : null
            },
            result =>
            {
                var content = new Markup(
                    $"[bold]{result.Path.EscapeMarkup()}[/]\n" +
                    $"Files:       {result.Files}\n" +
                    $"Diagnostics: {result.Diagnostics}\n" +
                    $"Checked:     {(result.Executable is null ? "source only; use --executable to check that the model binds" : "source and executable binding")}");
                var header = (success, result.Executable) switch
                {
                    (false, _) => " Invalid ",
                    (_, null) => " Valid ",
                    _ => " Valid and executable "
                };
                var panel = new Panel(content)
                    .Header(header)
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(success ? OutputFormatter.Success : OutputFormatter.Danger))
                    .Padding(1, 0);

                AnsiConsole.WriteLine();
                AnsiConsole.Write(panel);
                if (result.WholeApplication is { } whole)
                {
                    AnsiConsole.MarkupLine($"In scope {result.Scope!.EscapeMarkup()}: {result.Errors} error(s), {result.Warnings} warning(s); {result.DeclarationCount} declaration(s), {result.DependentDeclarationCount} direct dependent(s)");
                    AnsiConsole.MarkupLine($"Whole application: {whole.Errors} error(s), {whole.Warnings} warning(s)");
                    AnsiConsole.MarkupLine($"Affected scopes: {string.Join(", ", result.AffectedScopes!.Value.Select(scope => scope.Length == 0 ? "<application>" : scope)).EscapeMarkup()}");
                    AnsiConsole.MarkupLine($"Unresolved event consumers (cannot be attributed to a scope): {result.UnresolvedEventConsumers!.ReferenceCount}");
                    AnsiConsole.MarkupLine(result.DependencyCoverage!.EscapeMarkup());
                }
                if (result.Checks.Length > 0)
                {
                    AnsiConsole.MarkupLine($"Completeness: {result.CompletenessStatus}; {string.Join(", ", result.Checks).EscapeMarkup()}");
                }
                if (result.CompletenessNote is not null)
                {
                    AnsiConsole.MarkupLine(result.CompletenessNote.EscapeMarkup());
                }
            });
    }
}
