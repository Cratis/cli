// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

public partial class DiagnoseCommand
{
    static async Task<DiagnoseData> GatherScope(IServices services, DiagnoseData snapshot)
    {
        var store = snapshot.EventStore;
        var ns = snapshot.Namespace;
        var failures = new List<DiagnoseCheckFailure>();
        var findings = new List<DiagnoseFinding>();
        int active = 0, replaying = 0, suspended = 0, disconnected = 0, quarantined = 0;
        await Check("Observers", store, ns, failures, async () =>
        {
            var observers = (await services.Observers.GetObservers(new AllObserversRequest
            {
                EventStore = store,
                Namespace = ns
            })).ToList();
            active = observers.Count(x => x.RunningState == ObserverRunningState.Active);
            replaying = observers.Count(x => x.RunningState == ObserverRunningState.Replaying);
            suspended = observers.Count(x => x.RunningState == ObserverRunningState.Suspended);
            disconnected = observers.Count(x => x.RunningState == ObserverRunningState.Disconnected);
            quarantined = observers.Count(x => x.RunningState == ObserverRunningState.Quarantined);
            findings.AddRange(observers.Where(x => x.RunningState == ObserverRunningState.Quarantined)
                .Select(x => new DiagnoseFinding("Quarantined observer", store, ns, x.Id)));
        });

        var failedPartitions = 0;
        await Check("Failed partitions", store, ns, failures, async () =>
        {
            var partitions = (await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
            {
                EventStore = store,
                Namespace = ns
            })).ToList();
            failedPartitions = partitions.Count;
            findings.AddRange(partitions.Select(x => new DiagnoseFinding("Failed partition", store, ns, $"{x.ObserverId}/{x.Partition}")));
        });

        var recommendations = 0;
        await Check("Recommendations", store, ns, failures, async () =>
        {
            var result = (await services.Recommendations.GetRecommendations(new GetRecommendationsRequest
            {
                EventStore = store,
                Namespace = ns
            }).EnsureSuccess()).ToList();
            recommendations = result.Count;
            findings.AddRange(result.Select(x => new DiagnoseFinding("Recommendation", store, ns, x.Id.ToString())));
        });

        ulong? tail = null;
        await Check("Event sequence", store, ns, failures, async () =>
        {
            var result = await services.Sequences.TailSequenceNumber(new TailSequenceNumberRequest
            {
                EventStore = store,
                Namespace = ns,
                EventSequenceId = CliDefaults.DefaultEventSequenceId
            }).EnsureSuccess();
            tail = result.SequenceNumber == ulong.MaxValue ? null : result.SequenceNumber;
        });

        return snapshot with
        {
            ActiveObservers = active,
            ReplayingObservers = replaying,
            SuspendedObservers = suspended,
            DisconnectedObservers = disconnected,
            QuarantinedObservers = quarantined,
            FailedPartitions = failedPartitions,
            PendingRecommendations = recommendations,
            EventSequenceTail = tail,
            ChecksCouldNotRun = failures,
            Findings = findings
        };
    }
}
