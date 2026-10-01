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
        var scopes = new List<DiagnoseData>();
        var storesToCheck = settings.AllEventStores ? eventStores : [eventStore];
        if (settings.AllEventStores && storesChecked && eventStores.Count == 0)
        {
            failures.Add(new("Event stores", null, null, "No event stores were found to check"));
        }

        foreach (var store in storesToCheck)
        {
            var namespaces = new List<string> { ns };
            if (settings.AllNamespaces || settings.AllEventStores)
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
            }

            foreach (var name in namespaces)
            {
                scopes.Add(await GatherScope(services, snapshot with { EventStore = store, Namespace = name }));
            }
        }

        return snapshot with
        {
            EventStore = settings.AllEventStores ? "all event stores" : eventStore,
            Namespace = settings.AllNamespaces || settings.AllEventStores ? "all namespaces" : ns,
            ActiveObservers = scopes.Sum(x => x.ActiveObservers),
            ReplayingObservers = scopes.Sum(x => x.ReplayingObservers),
            SuspendedObservers = scopes.Sum(x => x.SuspendedObservers),
            DisconnectedObservers = scopes.Sum(x => x.DisconnectedObservers),
            QuarantinedObservers = scopes.Sum(x => x.QuarantinedObservers),
            FailedPartitions = scopes.Sum(x => x.FailedPartitions),
            PendingRecommendations = scopes.Sum(x => x.PendingRecommendations),
            EventSequenceTail = scopes.Count == 1 ? scopes[0].EventSequenceTail : null,
            ChecksCouldNotRun = [.. failures, .. scopes.SelectMany(x => x.ChecksCouldNotRun)],
            Findings = [.. scopes.SelectMany(x => x.Findings)],
            Scopes = scopes
        };
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
