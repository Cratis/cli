// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Validation;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Cratis Screenplay (<c language="csharp">.play</c>) documents and reports everything the compiler found, whatever wrote
/// them — <c language="csharp">screenplay generate</c>, <c language="csharp">prologue</c>, or a person.
/// </summary>
[LlmDescription("Compiles Cratis Screenplay (.play) documents and reports every diagnostic the compiler produces. Takes a root .play file together with its imports, or a folder in which case every .play file beneath it is compiled as one application. Nothing needs to be running. The validation gate is resolved per value: explicit command-line flag, then screenplay.json at the model root, then defaults. Invalid or unreadable gate files refuse validation with exit 1; --no-gate-file bypasses the file and its refusal. --print-gate prints the effective gate and sources without compiling. JSON results include gate. Use repeatable --check selections for opt-in structural completeness warnings; these are skipped when source compilation has errors and do not prove runtime completeness. Completeness and --executable can be checked together. Target admission is opt-in with --executable --target cratis; it uses the bundled Stage planner in memory without writing anything. Without --target validation stays target-neutral. --scope selects source diagnostics for a module, feature or slice and its direct dependents; the exit code covers only that set and whole-application counts are always reported separately. Check executable binding for the whole application separately; a file-declared scope with --executable exits 1. Diagnostics go to standard error, grouped by severity; errors fail validation, while warnings fail only with --warnings-as-errors (alias --warnaserror) or a declared warningsAsErrors gate. --open-questions report prints open model questions after compiler diagnostics; fail gates on them with exit 5. Notes are ignored unless the effective gate selects a mode and do not participate in --warnings-as-errors. Exit codes: 0 for valid source (and binding with --executable), or a printed gate; 1 for missing/invalid input, scope or gate file, or unreadable model notes; 5 for validation errors, warnings with warnings-as-errors, open questions in fail mode, unknown options or invalid --check/--open-questions selections; -1 (255 on POSIX) for CLI usage errors or unexpected failures.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("validate", "Validate Screenplay (.play) documents. Exit 0 valid, 1 invalid input or scope, 5 validation failed", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "validate")]
[CliExample("screenplay", "validate", "./MyApp.play")]
[CliExample("screenplay", "validate", "./plays")]
[CliExample("screenplay", "validate", "--executable", "./plays")]
[CliExample("screenplay", "validate", "--executable", "--target", "cratis", "--name", "MyApp", "./plays")]
[CliExample("screenplay", "validate", "./plays", "--check", "all")]
[CliExample("screenplay", "validate", "./plays", "--open-questions", "fail")]
[CliExample("screenplay", "validate", "./plays", "--no-gate-file")]
[CliExample("screenplay", "validate", "./plays", "--print-gate")]
[CliExample("screenplay", "validate", "./plays", "--check", "navigation", "--check", "field-origins", "--executable")]
[LlmOption("[PATH]", "string", "Root Screenplay (.play) file with its imports, or folder to compile every .play file beneath as one application. Defaults to the current directory.")]
[LlmOption("--warnings-as-errors", "boolean", "Treat compiler and completeness warnings as validation errors (exit 5). --warnaserror is an alias.")]
[LlmOption("--warnaserror", "boolean", "Alias of --warnings-as-errors; compiler and completeness warnings fail validation (exit 5).")]
[LlmOption("--scope", "string", "Case-sensitive Module[.Feature[.Slice]] address. Selects source diagnostics and direct dependents; whole-application counts remain visible. Unknown or ambiguous scopes fail with exit 1. Combine with --check, not --executable.")]
[LlmOption("--check", "string[]", "Repeatable; selections union. Accepts comma-separated data-bindings (PLAY0530, PLAY0531), input-surfaces (PLAY0532, PLAY0533), field-origins (PLAY0534), query-keys (PLAY0535), event-consumers (PLAY0536), navigation (PLAY0537), personas (PLAY0574–PLAY0576), purposes (PLAY0602–PLAY0606), privilege (PLAY0652), enum-pinning (PLAY0671), or all. Also accepts diagnostic codes. Skipped on source compilation errors. Findings are structural warnings, not proof of runtime completeness.")]
[LlmOption("--open-questions", "string", "Optional report or fail, not repeatable. Reports open model questions after diagnostics; fail exits 5 if any remain. Unreadable notes exit 1; compiler errors still exit 5. Absent option uses the declared gate mode, or ignores notes by default; --warnings-as-errors is unchanged. Honors --scope when identities exist; otherwise reports all questions as unscoped orphans.")]
[LlmOption("--no-gate-file", "boolean", "Ignore screenplay.json, including a refused file. Use explicit command-line values and defaults.")]
[LlmOption("--print-gate", "boolean", "Print the effective gate and commandLine, file or default sources, then exit 0 without compiling. A refused file exits 1 unless bypassed with --no-gate-file. Quiet prints nothing.")]
[LlmOption("--executable", "boolean", "Also bind the model into an executable semantic model; fails with binding diagnostics such as PLAY0268 when it does not bind. Without --target, never renders; never writes files. A pass without this option only means the source is valid.")]
[LlmOption("--target", "string", "Opt into bundled target admission after --executable binding. Accepts cratis. Without a target validation stays target-neutral; nothing is written.")]
[LlmOption("--name", "string", "Application name for target admission; defaults to the model-root workspace name, or source folder name. Ignored without --target.")]
[LlmOption("--no-render-admission", "boolean", "Skip opted-in target admission, retaining source and executable binding validation.")]
[LlmOutputAdvice("json-compact", "The summary goes to standard output and the diagnostics to standard error; json-compact makes both machine-readable.")]
public class ValidateScreenplayCommand : Command<ValidateScreenplaySettings>
{
    readonly IScreenplayValidation? _validation;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateScreenplayCommand"/> class.
    /// </summary>
    public ValidateScreenplayCommand()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateScreenplayCommand"/> class.
    /// </summary>
    /// <param name="validation">The validation to compile the documents with.</param>
    internal ValidateScreenplayCommand(IScreenplayValidation? validation)
    {
        _validation = validation;
    }

    /// <inheritdoc/>
    public override int Execute(CommandContext context, ValidateScreenplaySettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        if (!new ScreenplayValidationOptions(settings, format).Validate(context))
        {
            return ExitCodes.ValidationError;
        }

        var target = PlayFileTargetResolver.Resolve(settings.Path, Directory.GetCurrentDirectory());
        if (!target.IsResolved)
        {
            OutputFormatter.WriteError(format, target.Error!, target.Suggestion, ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var resolution = ScreenplayValidationGates.Resolve(target.Path!, settings, out var declared);
        if (resolution.Failure is { } failure)
        {
            ScreenplayValidationGates.Refuse(format, failure);
            return ExitCodes.NotFound;
        }
        var gate = resolution.Gate!;
        if (settings.PrintGate)
        {
            ScreenplayValidationGates.Print(format, gate);
            return ExitCodes.Success;
        }
        if (gate.Scope.Source == ValidationGateSource.File && settings.Executable)
        {
            OutputFormatter.WriteError(format, $"Scope declared in '{gate.GateFilePath}' cannot be combined with --executable.", "Check executable binding for the whole application separately, or use --no-gate-file", ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        return ValidateTarget(new ValidationRequest(settings, format, target.Path!, gate, declared), cancellationToken);
    }

    static int CompleteValidation(
        ValidationRequest request,
        ValidatedScreenplay validated,
        CompletenessChecks checks,
        CancellationToken cancellationToken)
    {
        var result = CheckTargetAdmission(request, validated, cancellationToken);
        validated = result.Validated;
        ScreenplayDiagnosticsWriter.Write(request.Format, validated.Diagnostics);
        var notesFolder = Directory.Exists(request.Path) ? request.Path : Path.GetDirectoryName(request.Path)!;
        if (request.Declared || request.Settings.NoGateFile) notesFolder = request.Gate.ModelRoot;
        var openQuestions = ScreenplayOpenQuestions.Read(notesFolder, request.Gate.OpenQuestions.Value, request.Gate.Scope.Value);
        ScreenplayOpenQuestions.Write(request.Format, openQuestions);
        return ReportResult(request, result, checks, openQuestions);
    }

    static TargetValidation CheckTargetAdmission(ValidationRequest request, ValidatedScreenplay validated, CancellationToken cancellationToken)
    {
        ArtifactRenderPlan? admission = null;
        IReadOnlyList<ScreenplayDiagnostic> admissionDiagnostics = [];
        if (!AdmissionRequested(request.Settings) || validated.Executable != true)
        {
            return new TargetValidation(validated, admission, admissionDiagnostics);
        }
        if (validated.BindingCompilation is not { Success: true } bound)
        {
            return new TargetValidation(validated, admission, admissionDiagnostics);
        }

        cancellationToken.ThrowIfCancellationRequested();
        (admission, admissionDiagnostics) = CratisRenderAdmission.Check(bound, validated.AttachmentDiagnostics, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var reported = validated.Diagnostics.ToHashSet();
        validated = validated with { Diagnostics = [.. validated.Diagnostics, .. admissionDiagnostics.Where(diagnostic => !reported.Contains(diagnostic))] };

        return new TargetValidation(validated, admission, admissionDiagnostics);
    }

    static int ReportResult(
        ValidationRequest request,
        TargetValidation result,
        CompletenessChecks checks,
        ModelNoteQuestionsOutcome? openQuestions)
    {
        var validated = result.Validated;
        var warningsAsErrors = request.Gate.WarningsAsErrors.Value;
        var exitCode = validated.Executable == false
            ? ExitCodes.ValidationError
            : ScreenplayDiagnostics.ExitCodeFor(validated.Diagnostics, warningsAsErrors);
        if (exitCode != ExitCodes.Success)
        {
            ScreenplayValidationResult.WriteFailure(request.Format, validated, warningsAsErrors);
            if (NeedsFailureSummary(request, result, checks, openQuestions))
            {
                WriteSummary(request, result, openQuestions, false);
            }
            return exitCode;
        }

        var notesExitCode = ScreenplayOpenQuestions.ExitCode(openQuestions, request.Gate.OpenQuestions.Value);
        WriteSummary(request, result, openQuestions, notesExitCode == ExitCodes.Success);
        return notesExitCode;
    }

    static bool NeedsFailureSummary(
        ValidationRequest request,
        TargetValidation result,
        CompletenessChecks checks,
        ModelNoteQuestionsOutcome? openQuestions) =>
        request.Settings.Target is not null || request.Declared || checks.Selected.Count > 0 || result.Validated.Scoped is not null || openQuestions is not null;

    static void WriteSummary(ValidationRequest request, TargetValidation result, ModelNoteQuestionsOutcome? openQuestions, bool success)
    {
        if (request.Settings.Target is not null)
        {
            WriteTargetResult(request, result, openQuestions, success);
        }
        else
        {
            ScreenplayValidationResult.Write(request.Format, request.Path, result.Validated, request.Gate, success, openQuestions);
        }
    }

    static void WriteTargetResult(ValidationRequest request, TargetValidation result, ModelNoteQuestionsOutcome? openQuestions, bool success)
    {
        var format = request.Format;
        var path = request.Path;
        var gate = request.Gate;
        var validated = result.Validated;
        var admission = result.Admission;
        var admissionDiagnostics = result.AdmissionDiagnostics;
        var skipped = request.Settings.NoRenderAdmission;
        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal))
        {
            if (success)
            {
                Console.WriteLine(path);
            }
            return;
        }

        var status = AdmissionStatus(skipped || validated.Executable != true, admission?.Success == true);
        var payload = new
        {
            Path = path,
            Valid = success,
            Gate = ScreenplayValidationGates.Json(gate),
            OpenQuestions = openQuestions,
            Files = validated.FileCount,
            Diagnostics = validated.Diagnostics.Count,
            Errors = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error),
            Warnings = validated.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning),
            Checked = "executable",
            validated.Executable,
            validated.CompletenessStatus,
            validated.CompletenessNote,
            Binding = new { validated.Executable },
            TargetAdmission = new
            {
                Target = CratisRendering.TargetId,
                Status = status,
                Diagnostics = admissionDiagnostics,
                Refusals = admission?.Refusals.Select(refusal => new { Artifact = refusal.Artifact.ToString(), refusal.Kind, refusal.Codes }).ToArray(),
                admission?.ImplementationGaps
            }
        };
        OutputFormatter.WriteObject(format, payload, result =>
        {
            AnsiConsole.MarkupLine($"[bold]{result.Path.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"Binding: {(validated.Executable == true ? "passed" : "failed")}");
            AnsiConsole.MarkupLine($"Target admission (cratis): {status}");
        });
    }

    static ValidatedScreenplay? ValidateSource(ValidationRequest request, IScreenplayValidation validation, CompletenessChecks checks)
    {
        if (request.Gate.Scope.Value is { } scope)
        {
            return ValidateScoped(request, validation, checks, scope);
        }

        return (checks.Selected.Count > 0, request.Settings.Executable) switch
        {
            (false, false) => validation.Validate(request.Path),
            (false, true) => validation.ValidateExecutable(request.Path),
            (true, false) => validation.Validate(request.Path, checks),
            (true, true) => validation.ValidateExecutable(request.Path, checks)
        };
    }

    static ValidatedScreenplay? ValidateScoped(ValidationRequest request, IScreenplayValidation validation, CompletenessChecks checks, string scope)
    {
        if (validation.TryValidateScoped(request.Path, scope, checks, out var scoped, out var error))
        {
            return scoped!;
        }

        var suggestion = ScopeFailureSuggestion(error!, request.Gate.Scope.Source);
        OutputFormatter.WriteError(request.Format, error!.Message, suggestion, ExitCodes.NotFoundCode);
        return null;
    }

    static string ScopeFailureSuggestion(ScopeSelectionError error, ValidationGateSource source) => error.Kind switch
    {
        ScopeSelectionErrorKind.InvalidPath => "Point the command at an existing .play file or folder",
        ScopeSelectionErrorKind.UnreadablePath => "Check that the application files and folders are readable",
        _ when source == ValidationGateSource.File => "Select an existing module, feature or slice address in screenplay.json, or use --no-gate-file",
        _ => "Select an existing module, feature or slice address in the application"
    };

    static bool AdmissionRequested(ValidateScreenplaySettings settings) => settings.Target is not null && !settings.NoRenderAdmission;

    static string AdmissionStatus(bool skipped, bool passed)
    {
        if (skipped)
        {
            return "skipped";
        }

        return passed ? "passed" : "failed";
    }

    int ValidateTarget(ValidationRequest request, CancellationToken cancellationToken)
    {
        var settings = request.Settings;
        var admissionRequested = AdmissionRequested(settings);
        var validation = _validation ?? (admissionRequested ? new ScreenplayValidation(settings.Name) : new ScreenplayValidation());
        var checks = ScreenplayValidationGates.Checks(request.Gate);
        var validated = ValidateSource(request, validation, checks);
        if (validated is null)
        {
            return ExitCodes.NotFound;
        }
        if (validated.FileCount == 0)
        {
            // Silently succeeding on a folder holding nothing turns the command into a no-op in CI, which is
            // exactly where it is trusted the most.
            OutputFormatter.WriteError(
                request.Format,
                $"No Screenplay ({PlayFileTargetResolver.Extension}) files found in '{request.Path}'",
                $"Point the command at a {PlayFileTargetResolver.Extension} file, or at a folder holding one",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        return CompleteValidation(request, validated, checks, cancellationToken);
    }

    sealed record ValidationRequest(ValidateScreenplaySettings Settings, string Format, string Path, EffectiveValidationGate Gate, bool Declared);

    sealed record TargetValidation(ValidatedScreenplay Validated, ArtifactRenderPlan? Admission, IReadOnlyList<ScreenplayDiagnostic> AdmissionDiagnostics);
}
