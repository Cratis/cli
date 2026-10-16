// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Writes a refused runtime admission as one result, retaining the original issues for machine readers.
/// </summary>
internal static class RunRefusalOutput
{
    internal static void Write(string format, StageRuntimeStatus status, RunSettings settings)
    {
        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            WriteJson(format, status, settings);
            return;
        }

        if (string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal))
        {
            foreach (var issue in status.Issues ?? [])
            {
                OutputFormatter.WriteError(format, Details(issue), errorCode: ExitCodes.ValidationErrorCode);
            }
        }
        else
        {
            WriteSummary(format, status, settings.Verbose);
        }

        var message = Message(status);
        var suggestion = status.SuggestionFor(settings);
        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            Console.Error.WriteLine(message);
            Console.Error.WriteLine(suggestion);
            return;
        }

        OutputFormatter.WriteError(format, message, suggestion, ExitCodes.ValidationErrorCode);
    }

    static void WriteJson(string format, StageRuntimeStatus status, RunSettings settings)
    {
        var result = new
        {
            error = ExitCodes.ValidationErrorCode,
            message = Message(status),
            suggestion = status.SuggestionFor(settings),
            engine = status.Engine,
            issues = (status.Issues ?? []).Select(issue => new { capability = issue.Capability, artifact = issue.Artifact, details = issue.Details }),
            notes = status.Notes ?? [],
            groups = StageRefusalGroups.Create(status.Issues ?? [], status.Notes).Select(group => new { category = group.Category, kind = group.Kind, count = group.Count, locations = group.Locations })
        };
        Console.Error.WriteLine(JsonSerializer.Serialize(result, string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ? OutputFormatter.IndentedJsonSerializerOptions : OutputFormatter.JsonSerializerOptions));
    }

    static void WriteSummary(string format, StageRuntimeStatus status, bool verbose)
    {
        StageDiagnosticOutput.WriteGroups(format, StageRefusalGroups.Create(status.Issues ?? [], status.Notes));
        if (verbose)
        {
            foreach (var issue in status.Issues ?? [])
            {
                StageDiagnosticOutput.WriteLine(format, Details(issue));
            }
        }
    }

    static string Message(StageRuntimeStatus status) => $"The model cannot run on the {status.Engine ?? "selected"} engine";

    static string Details(StageRuntimeIssue issue) => $"Unsupported({issue.Capability}) {issue.Artifact}: {issue.Details}";
}
