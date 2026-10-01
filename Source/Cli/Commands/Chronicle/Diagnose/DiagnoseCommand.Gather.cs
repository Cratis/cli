// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

public partial class DiagnoseCommand
{
    internal static async Task<DiagnoseData> Gather(IServices services, DiagnoseSettings settings)
    {
        var eventStore = settings.ResolveEventStore();
        var ns = settings.ResolveNamespace();
        var connectionString = settings.ResolveConnectionString();
        var failures = new List<DiagnoseCheckFailure>();
        string? serverVersion = null;
        var serverReachable = await Check("Connection", null, null, failures, async () =>
            serverVersion = (await services.Server.GetVersionInfo()).Version);

        // The package feed is advisory, not a Chronicle health check.
        string? latestServerVersion = null;
        if (serverVersion is not null)
        {
            try
            {
                using var updateCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                latestServerVersion = await CheckLatestServerVersion(
                    serverVersion,
                    (packageId, version, refreshes, token) => UpdateChecker.CheckForUpdate(packageId, version, false, refreshes, token),
                    updateCts.Token);
            }
            catch (Exception)
            {
                // An unavailable package feed does not make the Chronicle server unhealthy.
                latestServerVersion = null;
            }
        }

        var eventStores = new List<string>();
        var storesChecked = await Check("Event stores", null, null, failures, async () =>
        {
            var stores = await services.EventStores.AllEventStores().EnsureSuccess();
            eventStores = [.. stores.Select(x => x.Name).Distinct(StringComparer.Ordinal)];
        });
        var snapshot = new DiagnoseData(connectionString, eventStore, ns, serverReachable, serverVersion, latestServerVersion, eventStores, 0, 0, 0, 0, 0, 0, null, DateTimeOffset.Now);
        var isAggregate = settings.AllNamespaces || settings.AllEventStores;
        var scopes = new List<DiagnoseData>();
        var storesToCheck = settings.AllEventStores ? eventStores : [eventStore];
        if (settings.AllEventStores && storesChecked && eventStores.Count == 0)
        {
            failures.Add(new("Event stores", null, null, "No event stores were found to check"));
        }

        if (settings.AllEventStores && eventStores.Count == 0)
        {
            var reason = storesChecked
                ? "skipped: event-store discovery found no stores"
                : "skipped: event-store discovery failed";
            failures.Add(new("Namespaces", null, null, reason));
            SkipScopeChecks(failures, null, reason);
        }

        foreach (var store in storesToCheck)
        {
            var namespaces = new List<string> { ns };
            if (isAggregate)
            {
                namespaces = [];
                var namespacesChecked = await Check("Namespaces", store, null, failures, async () =>
                {
                    var result = await services.Namespaces.AllNamespaces(new AllNamespacesRequest { EventStore = store }).EnsureSuccess();
                    namespaces = [.. result.Select(x => x.Name).Distinct(StringComparer.Ordinal)];
                });
                if (namespacesChecked && namespaces.Count == 0)
                {
                    failures.Add(new("Namespaces", store, null, "No namespaces were found to check"));
                }

                if (namespaces.Count == 0)
                {
                    SkipScopeChecks(failures, store, namespacesChecked
                        ? "skipped: namespace discovery found no namespaces"
                        : "skipped: namespace discovery failed");
                }
            }

            foreach (var name in namespaces)
            {
                scopes.Add(await GatherScope(services, snapshot with { EventStore = store, Namespace = name }));
            }
        }

        return snapshot with
        {
            IsAggregate = isAggregate,
            EventStore = settings.AllEventStores ? "all event stores" : eventStore,
            Namespace = isAggregate ? "all namespaces" : ns,
            TotalObservers = scopes.Sum(x => x.TotalObservers),
            ActiveObservers = scopes.Sum(x => x.ActiveObservers),
            ReplayingObservers = scopes.Sum(x => x.ReplayingObservers),
            SuspendedObservers = scopes.Sum(x => x.SuspendedObservers),
            DisconnectedObservers = scopes.Sum(x => x.DisconnectedObservers),
            QuarantinedObservers = scopes.Sum(x => x.QuarantinedObservers),
            FailedPartitions = scopes.Sum(x => x.FailedPartitions),
            PendingRecommendations = scopes.Sum(x => x.PendingRecommendations),
            EventSequenceTail = !isAggregate && scopes.Count == 1 ? scopes[0].EventSequenceTail : null,
            ChecksCouldNotRun = [.. failures, .. scopes.SelectMany(x => x.ChecksCouldNotRun)],
            Findings = [.. scopes.SelectMany(x => x.Findings)],
            Scopes = scopes
        };
    }

    static void SkipScopeChecks(List<DiagnoseCheckFailure> failures, string? eventStore, string reason)
    {
        foreach (var check in new[] { "Observers", "Failed partitions", "Recommendations", "Event sequence" })
        {
            failures.Add(new(check, eventStore, null, reason));
        }
    }

    static async Task<bool> Check(string check, string? eventStore, string? ns, List<DiagnoseCheckFailure> failures, Func<Task> run)
    {
        try
        {
            await run();
            return true;
        }
        catch (Exception ex)
        {
            failures.Add(new(check, eventStore, ns, ex.Message));
            return false;
        }
    }
}
