// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Validation;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Compiles Cratis Screenplay (<c language="csharp">.play</c>) documents and reports everything the compiler found, whatever wrote
/// them — <c language="csharp">screenplay generate</c>, <c language="csharp">prologue</c>, or a person.
/// </summary>
[LlmDescription("Compiles Cratis Screenplay (.play) documents and reports every diagnostic the compiler produces. Takes a root .play file together with its imports, or a folder in which case every .play file beneath it is compiled as one application. Nothing needs to be running. The validation gate is resolved per value: explicit command-line flag, then screenplay.json at the model root, then defaults. Invalid or unreadable gate files refuse validation with exit 1; --no-gate-file bypasses the file and its refusal. --print-gate prints the effective gate and sources without compiling. JSON results include gate. Use repeatable --check selections for opt-in structural completeness warnings; these are skipped when source compilation has errors and do not prove runtime completeness. Completeness and --executable can be checked together. --scope selects source diagnostics for a module, feature or slice and its direct dependents; the exit code covers only that set and whole-application counts are always reported separately. Check executable binding for the whole application separately; a file-declared scope with --executable exits 1. Diagnostics go to standard error, grouped by severity; errors fail validation, while warnings fail only with --warnings-as-errors (alias --warnaserror) or a declared warningsAsErrors gate. --open-questions report prints open model questions after compiler diagnostics; fail gates on them with exit 5. Notes are ignored unless the effective gate selects a mode and do not participate in --warnings-as-errors. Exit codes: 0 for valid source (and binding with --executable), or a printed gate; 1 for missing/invalid input, scope or gate file, or unreadable model notes; 5 for validation errors, warnings with warnings-as-errors, open questions in fail mode, unknown options or invalid --check/--open-questions selections; -1 (255 on POSIX) for CLI usage errors or unexpected failures.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("validate", "Validate Screenplay (.play) documents. Exit 0 valid, 1 invalid input or scope, 5 validation failed", Branch = typeof(ScreenplayBranch))]
[CliExample("screenplay", "validate")]
[CliExample("screenplay", "validate", "./MyApp.play")]
[CliExample("screenplay", "validate", "./plays")]
[CliExample("screenplay", "validate", "--executable", "./plays")]
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
        if (context.Remaining.Parsed.Count > 0 || context.Remaining.Raw.Count > 0)
        {
            var option = context.Remaining.Raw.Count > 0 ? context.Remaining.Raw[0] : context.Remaining.Parsed.First().Key;
            OutputFormatter.WriteError(format, $"Unknown option '{option}'.", "Run 'cratis screenplay validate --help' for usage", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        if (settings.OpenQuestions.Length > 1 || settings.OpenQuestions.Any(mode => mode is not ("report" or "fail")))
        {
            OutputFormatter.WriteError(format, "--open-questions requires report or fail and cannot be repeated.", "Run 'cratis screenplay validate --help' for usage", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var checkValidation = settings.ValidateChecks();
        if (!checkValidation.Successful)
        {
            OutputFormatter.WriteError(format, checkValidation.Message!, "Run 'cratis screenplay validate --help' for usage", ExitCodes.ValidationErrorCode);
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

        var checks = ScreenplayValidationGates.Checks(gate);
        ValidatedScreenplay validated;
        if (gate.Scope.Value is { } scope)
        {
            if (!_validation.TryValidateScoped(target.Path!, scope, checks, out var scoped, out var error))
            {
                var suggestion = error!.Kind switch
                {
                    ScopeSelectionErrorKind.InvalidPath => "Point the command at an existing .play file or folder",
                    ScopeSelectionErrorKind.UnreadablePath => "Check that the application files and folders are readable",
                    _ when gate.Scope.Source == ValidationGateSource.File => "Select an existing module, feature or slice address in screenplay.json, or use --no-gate-file",
                    _ => "Select an existing module, feature or slice address in the application"
                };
                OutputFormatter.WriteError(format, error.Message, suggestion, ExitCodes.NotFoundCode);
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
        var notesFolder = Directory.Exists(target.Path!) ? target.Path! : Path.GetDirectoryName(target.Path)!;
        if (declared || settings.NoGateFile) notesFolder = gate.ModelRoot;
        var openQuestions = ScreenplayOpenQuestions.Read(notesFolder, gate.OpenQuestions.Value, gate.Scope.Value);
        ScreenplayOpenQuestions.Write(format, openQuestions);

        var warningsAsErrors = gate.WarningsAsErrors.Value;
        var exitCode = validated.Executable == false
            ? ExitCodes.ValidationError
            : ScreenplayDiagnostics.ExitCodeFor(validated.Diagnostics, warningsAsErrors);
        if (exitCode != ExitCodes.Success)
        {
            ScreenplayValidationResult.WriteFailure(format, validated, warningsAsErrors);
            if (declared || checks.Selected.Count > 0 || validated.Scoped is not null || openQuestions is not null)
            {
                ScreenplayValidationResult.Write(format, target.Path!, validated, gate, false, openQuestions);
            }
            return exitCode;
        }

        var notesExitCode = ScreenplayOpenQuestions.ExitCode(openQuestions, gate.OpenQuestions.Value);
        ScreenplayValidationResult.Write(format, target.Path!, validated, gate, notesExitCode == ExitCodes.Success, openQuestions);
        return notesExitCode;
    }
}
