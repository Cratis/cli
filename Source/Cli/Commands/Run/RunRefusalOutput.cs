// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Writes a refused runtime admission as one result, retaining the original issues for machine readers.
/// </summary>
internal static class RunRefusalOutput
{
    internal static void Write(string format, StageRuntimeStatus status, RunSettings settings, string? fallbackReason)
    {
        var message = $"The model cannot run on the {status.Engine ?? "selected"} engine";
        var suggestion = status.SuggestionFor(settings);
        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            var result = new
            {
                error = ExitCodes.ValidationErrorCode,
                message,
                suggestion,
                engine = status.Engine,
                issues = (status.Issues ?? []).Select(issue => new { capability = issue.Capability, artifact = issue.Artifact, details = issue.Details }),
                engineFallback = RunOutput.FallbackDetails(fallbackReason)
            };
            Console.Error.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions(OutputFormatter.JsonSerializerOptions)
            {
                WriteIndented = string.Equals(format, OutputFormats.Json, StringComparison.Ordinal)
            }));
            return;
        }

        foreach (var issue in status.Issues ?? [])
        {
            OutputFormatter.WriteError(format, $"Unsupported({issue.Capability}) {issue.Artifact}: {issue.Details}", errorCode: ExitCodes.ValidationErrorCode);
        }

        OutputFormatter.WriteError(format, message, suggestion, ExitCodes.ValidationErrorCode);
    }
}
