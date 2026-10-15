// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Validation;

namespace Cratis.Cli.Commands.Screenplay;

internal static class ScreenplayValidationGates
{
    internal static ValidationGateResolution Resolve(string target, ValidateScreenplaySettings settings, out bool declared)
    {
        declared = false;
        try
        {
            var root = ValidationGateRoot.Resolve(target);
            var read = settings.NoGateFile
                ? new ValidationGateReadResult(root, Path.Combine(root, "screenplay.json"), null, null)
                : ScreenplayValidationGate.Read(root);
            declared = read.Declaration is not null;
            var commandLine = new DeclaredValidationGate(
                settings.Checks.Length > 0 ? [.. settings.Checks] : null,
                settings.WarningsAsErrors ? true : null,
                settings.Scope,
                settings.OpenQuestions.FirstOrDefault());
            return ValidationGateResolver.Resolve(read, commandLine, settings.NoGateFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(null, new(ValidationGateFailureKind.Unreadable, target, 0, 0, $"Could not resolve or read the validation gate: {exception.Message}"));
        }
    }

    internal static CompletenessChecks Checks(EffectiveValidationGate gate)
    {
        var checks = CompletenessChecks.None;
        foreach (var name in gate.Checks.Value)
        {
            if (!CompletenessChecks.TryParse(name, out var parsed)) throw new UnrecognizedValidationGateChecks(name);
            checks = new(checks.Selected.Union(parsed.Selected));
        }

        return checks;
    }

    internal static object Json(EffectiveValidationGate gate) => new
    {
        gate.ModelRoot,
        gate.GateFilePath,
        Checks = Value(gate.Checks),
        WarningsAsErrors = Value(gate.WarningsAsErrors),
        Scope = Value(gate.Scope),
        OpenQuestions = Value(gate.OpenQuestions)
    };

    internal static void Print(string format, EffectiveValidationGate gate) => OutputFormatter.WriteObject(format, Json(gate), _ =>
    {
        Console.WriteLine($"Model root: {gate.ModelRoot}");
        Console.WriteLine($"Gate file: {gate.GateFilePath}");
        Console.WriteLine($"Checks: {string.Join(", ", gate.Checks.Value)} (source: {Source(gate.Checks.Source)})");
        Console.WriteLine($"Warnings as errors: {gate.WarningsAsErrors.Value.ToString().ToLowerInvariant()} (source: {Source(gate.WarningsAsErrors.Source)})");
        Console.WriteLine($"Scope: {gate.Scope.Value ?? "none"} (source: {Source(gate.Scope.Source)})");
        Console.WriteLine($"Open questions: {gate.OpenQuestions.Value ?? "not evaluated"} (source: {Source(gate.OpenQuestions.Source)})");
    });

    internal static void Refuse(string format, ValidationGateFailure failure)
    {
        var location = failure.Line > 0 && failure.Column > 0 ? $"{failure.Path}:{failure.Line}:{failure.Column}" : failure.Path;
        OutputFormatter.WriteError(format, $"{location}: {failure.Reason}", "Fix screenplay.json or use --no-gate-file to ignore it", ExitCodes.NotFoundCode);
    }

    static GateValue<T> Value<T>(ValidationGateValue<T> value) => new(value.Value, Source(value.Source));

    static string Source(ValidationGateSource source) => source switch
    {
        ValidationGateSource.CommandLine => "commandLine",
        ValidationGateSource.File => "file",
        _ => "default"
    };

    sealed record GateValue<T>([property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] T Value, string Source);
}
