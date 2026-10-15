// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Validation;

namespace Cratis.Cli.Commands.Screenplay;

internal static class ScreenplayValidationResult
{
    internal static void Write(string format, string targetPath, ValidatedScreenplay validated, EffectiveValidationGate gate, bool success, ModelNoteQuestionsOutcome? openQuestions)
    {
        var warningsAsErrors = gate.WarningsAsErrors.Value;
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
                Gate = ScreenplayValidationGates.Json(gate),
                OpenQuestions = openQuestions,
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
                    var outsideCount = whole.Errors + whole.Warnings - result.Errors - result.Warnings;
                    var wholeSummary = $"Whole application: {whole.Errors} error(s), {whole.Warnings} warning(s) ({outsideCount} outside the reported set)";
                    AnsiConsole.Write(new Text(wholeSummary, whole.Valid ? Style.Plain : new Style(OutputFormatter.Danger)));
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"Affected scopes: {string.Join(", ", result.AffectedScopes!.Value.Select(scope => scope.Length == 0 ? "<application>" : scope)).EscapeMarkup()}");
                    var consumers = result.UnresolvedEventConsumers!;
                    var consumerScopes = consumers.Scopes.Length == 0 ? "none" : string.Join(", ", consumers.Scopes.Select(scope => scope.Length == 0 ? "<application>" : scope));
                    AnsiConsole.MarkupLine($"Unresolved event consumers (cannot be attributed to a scope): {consumers.ReferenceCount} reference(s) in {consumerScopes.EscapeMarkup()}");
                    AnsiConsole.MarkupLine($"Possibly affected: {result.PossiblyAffectedReferenceCount} other unresolved reference(s) outside the reported declarations");
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

    internal static void WriteFailure(string format, ValidatedScreenplay validated, bool warningsAsErrors)
    {
        var errors = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
        var warnings = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning);
        var message = warningsAsErrors
            ? $"Validation reported {errors} error(s) and {warnings} warning(s)"
            : $"Validation reported {errors} error(s)";
        if (validated.Executable == false && errors == 0)
        {
            message = "The model does not bind into an executable semantic model";
        }

        var suggestion = warningsAsErrors
            ? "Fix the reported errors and warnings in the Screenplay document"
            : "Fix the reported errors in the Screenplay document";
        OutputFormatter.WriteError(format, message, suggestion, ExitCodes.ValidationErrorCode);
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
}
