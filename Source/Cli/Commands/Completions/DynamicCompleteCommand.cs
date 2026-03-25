// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Hidden internal command that outputs resource identifiers for dynamic shell completion.
/// Not shown in help or listed commands.
/// Returns empty output on any error to avoid breaking shell completion.
/// </summary>
[CliCommand("_complete", "(internal) dynamic completion helper", IsHidden = true, ExcludeFromLlm = true)]
public class DynamicCompleteCommand : ChronicleCommand<DynamicCompleteSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, DynamicCompleteSettings settings, string format)
    {
        try
        {
            var eventStore = settings.ResolveEventStore();
            var ns = settings.ResolveNamespace();

            switch (settings.Context.ToLowerInvariant())
            {
                case "observers":
                    var observers = await services.Observers.GetObservers(new AllObserversRequest
                    {
                        EventStore = eventStore,
                        Namespace = ns
                    });
                    foreach (var obs in observers ?? [])
                    {
                        Console.WriteLine(obs.Id);
                    }

                    break;

                case "jobs":
                    var jobs = await services.Jobs.GetJobs(new GetJobsRequest
                    {
                        EventStore = eventStore,
                        Namespace = ns
                    });
                    foreach (var job in jobs ?? [])
                    {
                        Console.WriteLine(job.Id.ToString());
                    }

                    break;

                case "read-models":
                    var response = await services.ReadModels.GetDefinitions(new GetDefinitionsRequest
                    {
                        EventStore = eventStore
                    });
                    foreach (var rm in response.ReadModels ?? [])
                    {
                        Console.WriteLine(rm.Type?.Identifier ?? rm.DisplayName ?? string.Empty);
                    }

                    break;

                case "event-types":
                    var types = await services.EventTypes.GetAll(new GetAllEventTypesRequest
                    {
                        EventStore = eventStore
                    });
                    foreach (var et in types ?? [])
                    {
                        Console.WriteLine(et.Id);
                    }

                    break;

                case "event-stores":
                    var stores = await services.EventStores.GetEventStores();
                    foreach (var store in stores ?? [])
                    {
                        Console.WriteLine(store);
                    }

                    break;
            }
        }
        catch
        {
            // Silently ignore all errors — shell completion must not break on server failure.
        }

        return ExitCodes.Success;
    }
}
