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
        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            WriteJson(format, status, settings, fallbackReason);
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
            WriteSummary(format, status.Issues ?? [], settings.Verbose);
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

    static void WriteJson(string format, StageRuntimeStatus status, RunSettings settings, string? fallbackReason)
    {
        var result = new
        {
            error = ExitCodes.ValidationErrorCode,
            message = Message(status),
            suggestion = status.SuggestionFor(settings),
            engine = status.Engine,
            issues = (status.Issues ?? []).Select(issue => new { capability = issue.Capability, artifact = issue.Artifact, details = issue.Details }),
            groups = StageRefusalGroups.Create(status.Issues ?? []).Select(group => new { category = group.Category, kind = group.Kind, count = group.Count, locations = group.Locations }),
            engineFallback = RunOutput.FallbackDetails(fallbackReason)
        };
        Console.Error.WriteLine(JsonSerializer.Serialize(result, string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ? OutputFormatter.IndentedJsonSerializerOptions : OutputFormatter.JsonSerializerOptions));
    }

    static void WriteSummary(string format, IReadOnlyList<StageRuntimeIssue> issues, bool verbose)
    {
        foreach (var category in StageRefusalGroups.Create(issues).GroupBy(group => group.Category))
        {
            WriteLine(format, $"{category.Key} ({category.Sum(group => group.Count)})", heading: true);
            foreach (var group in category)
            {
                WriteLine(format, group.Summary);
            }
        }

        if (verbose)
        {
            foreach (var issue in issues)
            {
                WriteLine(format, Details(issue));
            }
        }
    }

    static string Message(StageRuntimeStatus status) => $"The model cannot run on the {status.Engine ?? "selected"} engine";

    static string Details(StageRuntimeIssue issue) => $"Unsupported({issue.Capability}) {issue.Artifact}: {issue.Details}";

    static void WriteLine(string format, string text, bool heading = false)
    {
        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            Console.WriteLine(text);
            return;
        }

        AnsiConsole.MarkupLine(heading ? $"  [bold]{text.EscapeMarkup()}[/]" : $"    {text.EscapeMarkup()}");
    }
}
