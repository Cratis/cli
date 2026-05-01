// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.FailedPartitions;

/// <summary>
/// Lists failed partitions.
/// </summary>
[CliCommand("list", "List failed partitions", Branch = typeof(ChronicleBranch.FailedPartitions))]
[CliExample("chronicle", "failed-partitions", "list")]
[CliExample("chronicle", "failed-partitions", "list", "--observer", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOutputAdvice("plain", "When empty, JSON is smaller (2B vs 33B). With data, use plain for consistency.")]
[LlmOption("--observer", "string", "Filter by observer identifier")]
public class ListFailedPartitionsCommand : ChronicleCommand<ListFailedPartitionsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ListFailedPartitionsSettings settings, string format)
    {
        var failedPartitions = await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId
        });

        var list = failedPartitions.ToList();

        OutputFormatter.Write(
            format,
            list,
            ["Id", "ObserverId", "Partition", "Attempts", "LastFailedSeq#"],
            fp =>
            {
                var lastAttempt = fp.Attempts.MaxBy(a => (DateTimeOffset?)a.Occurred ?? DateTimeOffset.MinValue);
                return
                [
                    fp.Id.ToString(),
                    fp.ObserverId,
                    fp.Partition,
                    fp.Attempts.Count().ToString(),
                    lastAttempt?.SequenceNumber.ToString() ?? string.Empty
                ];
            });

        return ExitCodes.Success;
    }
}
