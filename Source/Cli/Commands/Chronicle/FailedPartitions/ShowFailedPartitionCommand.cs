// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.FailedPartitions;

/// <summary>
/// Shows detailed information about a specific failed partition, including all retry attempts and stack traces.
/// </summary>
[CliCommand("show", "Show detailed information about a specific failed partition", Branch = typeof(ChronicleBranch.FailedPartitions))]
[CliExample("chronicle", "failed-partitions", "show", "550e8400-e29b-41d4-a716-446655440000", "my-partition")]
[LlmOutputAdvice("json", "JSON contains full error messages and stack traces. Use JSON for structured error analysis.")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
[LlmOption("<PARTITION>", "string", "Partition key (typically an event source ID, from 'cratis failed-partitions list') (positional)")]
public class ShowFailedPartitionCommand : ChronicleCommand<ShowFailedPartitionSettings>
{
    const int MaxAttemptsDisplayed = 5;
    const int MaxStackTraceLines = 5;

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ShowFailedPartitionSettings settings, string format)
    {
        var failedPartitions = await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId
        });

        var match = failedPartitions.FirstOrDefault(fp =>
            string.Equals(fp.Partition, settings.Partition, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            OutputFormatter.WriteError(
                format,
                $"No failed partition '{settings.Partition}' found for observer '{settings.ObserverId}'",
                "Use 'cratis failed-partitions list' to see all failed partitions",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        var attempts = (match.Attempts ?? []).ToList();

        OutputFormatter.WriteObject(
            format,
            new
            {
                match.Id,
                match.ObserverId,
                match.Partition,
                AttemptCount = attempts.Count,
                Attempts = attempts.Select(a => new
                {
                    Occurred = a.Occurred?.ToString() ?? string.Empty,
                    a.SequenceNumber,
                    Messages = (a.Messages ?? []).ToArray(),
                    a.StackTrace
                }).ToArray()
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]FailedPartition:[/] {data.Id}");
                AnsiConsole.MarkupLine($"[bold]Observer:[/]        {data.ObserverId.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Partition:[/]       {data.Partition.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Attempts:[/]        {data.AttemptCount}");
                AnsiConsole.WriteLine();

                var displayAttempts = settings.Detailed
                    ? data.Attempts
                    : data.Attempts.Take(MaxAttemptsDisplayed).ToArray();
                var hiddenAttempts = data.AttemptCount - displayAttempts.Length;

                foreach (var attempt in displayAttempts)
                {
                    AnsiConsole.MarkupLine($"  [yellow]--- Attempt at {attempt.Occurred.EscapeMarkup()} (Seq# {attempt.SequenceNumber}) ---[/]");
                    foreach (var message in attempt.Messages)
                    {
                        AnsiConsole.MarkupLine($"  [red]{message.EscapeMarkup()}[/]");
                    }

                    if (!string.IsNullOrWhiteSpace(attempt.StackTrace))
                    {
                        AnsiConsole.MarkupLine("[dim]  StackTrace:[/]");
                        if (settings.Detailed)
                        {
                            AnsiConsole.WriteLine($"  {attempt.StackTrace}");
                        }
                        else
                        {
                            var lines = attempt.StackTrace.Split('\n');
                            var displayLines = lines.Take(MaxStackTraceLines);
                            AnsiConsole.WriteLine($"  {string.Join("\n  ", displayLines)}");
                            if (lines.Length > MaxStackTraceLines)
                            {
                                AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]… ({lines.Length - MaxStackTraceLines} more lines hidden, use --detailed to expand)[/]");
                            }
                        }
                    }

                    AnsiConsole.WriteLine();
                }

                if (hiddenAttempts > 0)
                {
                    AnsiConsole.MarkupLine($"[{OutputFormatter.Muted.ToMarkup()}]… ({hiddenAttempts} more error(s) hidden, use --detailed to expand)[/]");
                }
            });

        return ExitCodes.Success;
    }
}
