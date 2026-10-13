// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.Commands.Screenplay;

internal static class ScreenplayOpenQuestions
{
    internal static ModelNoteQuestionsOutcome? Read(string target, ValidateScreenplaySettings settings) => settings.OpenQuestions.Length == 0
        ? null
        : ModelNoteQuestions.Read(Directory.Exists(target) ? target : Path.GetDirectoryName(target)!, settings.Scope);

    internal static int ExitCode(ModelNoteQuestionsOutcome? outcome, ValidateScreenplaySettings settings) => outcome?.GetExitCode(settings.OpenQuestions.FirstOrDefault() == "fail") switch
    {
        1 => ExitCodes.ValidationError,
        2 => ExitCodes.NotFound,
        _ => ExitCodes.Success
    };

    internal static void Write(string format, ModelNoteQuestionsOutcome? outcome)
    {
        if (outcome is null) return;
        if (outcome.FailureKind is not null)
        {
            OutputFormatter.WriteError(format, $"{outcome.FailureKind}: {outcome.Reason}", "Check that the model notes and persisted identities are readable", ExitCodes.NotFoundCode);
            return;
        }
        if (new[] { OutputFormats.Json, OutputFormats.JsonCompact, OutputFormats.JsonQuiet, OutputFormats.Quiet }.Contains(format, StringComparer.Ordinal)) return;
        if (outcome.IdentityStatus == "absent")
        {
            Console.WriteLine("Identity state absent: .screenplay/identities.json is missing; notes cannot be scoped. Reporting open questions unscoped as orphaned (identity-state-absent).");
        }
        foreach (var finding in outcome.Findings)
        {
            Console.WriteLine($"{Address(finding, outcome.OrphanReason)}: {finding.Severity} {finding.Code}: {finding.Message} ({finding.NoteId})");
        }
        Console.WriteLine($"Model notes: {outcome.Findings.Length} open question(s){(outcome.EffectiveScope is null ? string.Empty : " in scope")}");
    }

    static string Address(ModelNotesFinding finding, string? orphanReason)
    {
        if (finding.Address is not null)
        {
            return string.Join('.', finding.Address.Parts.Where(part => part.Kind != "Application").Select(part => part.Key));
        }

        return orphanReason is null ? "<orphaned>" : $"<orphaned: {orphanReason}>";
    }
}
