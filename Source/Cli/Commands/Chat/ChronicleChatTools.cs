// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Identities;
using Cratis.Cli.Commands.Chronicle;
using Microsoft.Extensions.AI;
using ReplayPartitionContract = Cratis.Chronicle.Contracts.Observation.ReplayPartition;
using RetryPartitionContract = Cratis.Chronicle.Contracts.Observation.RetryPartition;

namespace Cratis.Cli.Commands.Chat;

/// <summary>
/// Defines AI tools that wrap Chronicle gRPC service calls for use in chat conversations.
/// Tool responses are projected to slim DTOs to minimize token usage.
/// Full detail is available via dedicated "show" tools.
/// </summary>
public static class ChronicleChatTools
{
    /// <summary>
    /// Names of tools that perform write/destructive operations and require user confirmation.
    /// </summary>
    public static readonly IReadOnlySet<string> WriteToolNames = new HashSet<string>
    {
        "replay_observer",
        "replay_partition",
        "retry_partition",
        "perform_recommendation"
    };

    const int MaxListItems = 100;

    /// <summary>
    /// Creates all Chronicle AI tools bound to the given gRPC services.
    /// </summary>
    /// <param name="services">The gRPC service proxies.</param>
    /// <returns>A list of AI tools.</returns>
    public static IReadOnlyList<AITool> Create(IServices services)
    {
        var eventStore = ResolveEventStore();
        var ns = ResolveNamespace();

        return
        [

            // ── Event stores & namespaces ──────────────────────────────────────
            AIFunctionFactory.Create(
                ([Description("Unused")] string? unused = null) =>
                    SerializeAsync(services.EventStores.GetEventStores()),
                "list_event_stores",
                "List all event store names on the Chronicle server."),

            AIFunctionFactory.Create(
                ([Description("Event store name (optional)")] string? eventStoreName = null) =>
                    SerializeAsync(services.Namespaces.GetNamespaces(new GetNamespacesRequest
                    {
                        EventStore = eventStoreName ?? eventStore
                    })),
                "list_namespaces",
                "List namespace names in an event store."),

            // ── Event types ────────────────────────────────────────────────────
            AIFunctionFactory.Create(
                ([Description("Event store name (optional)")] string? eventStoreName = null) =>
                    SerializeAsync(
                        services.EventTypes.GetAllRegistrations(new GetAllEventTypesRequest
                        {
                            EventStore = eventStoreName ?? eventStore
                        }),
                        r => new
                        {
                            id = r.Type.Id,
                            generation = r.Type.Generation,
                            owner = r.Owner,
                            source = r.Source
                        }),
                "list_event_types",
                "List registered event types. Returns id, generation, owner, source — schema omitted to save tokens. Use show_event_type for the full schema."),

            AIFunctionFactory.Create(
                (
                    [Description("Event type id (name)")] string eventTypeId,
                    [Description("Event store name (optional)")] string? eventStoreName = null) =>
                    FindEventTypeAsync(services, eventStoreName ?? eventStore, eventTypeId),
                "show_event_type",
                "Show the full JSON schema and metadata for a single event type."),

            // ── Observers ──────────────────────────────────────────────────────
            AIFunctionFactory.Create(
                (
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    SerializeAsync(
                        services.Observers.GetObservers(new AllObserversRequest
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns
                        }),
                        o => new
                        {
                            id = o.Id,
                            type = o.Type.ToString(),
                            state = o.RunningState.ToString(),
                            nextSequenceNumber = o.NextEventSequenceNumber,
                            lastHandledSequenceNumber = o.LastHandledEventSequenceNumber,
                            isSubscribed = o.IsSubscribed
                        }),
                "list_observers",
                "List all observers (reactors, reducers, projections) with their state. Use show_observer for event type subscriptions."),

            AIFunctionFactory.Create(
                (
                    [Description("Observer ID")] string observerId,
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    FindObserverAsync(services, eventStoreName ?? eventStore, namespaceName ?? ns, observerId),
                "show_observer",
                "Show detailed information about a specific observer including subscribed event types."),

            // ── Failed partitions ──────────────────────────────────────────────
            AIFunctionFactory.Create(
                (
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null,
                    [Description("Filter by observer ID (optional)")] string? observerId = null) =>
                    SerializeAsync(
                        services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns,
                            ObserverId = observerId ?? string.Empty
                        }),
                        fp => new
                        {
                            id = fp.Id,
                            observerId = fp.ObserverId,
                            partition = fp.Partition,
                            attemptCount = fp.Attempts.Count(),
                            lastError = fp.Attempts.Any()
                                ? TruncateMessage(fp.Attempts.Last().Messages.FirstOrDefault() ?? string.Empty)
                                : string.Empty
                        }),
                "list_failed_partitions",
                "List failed partitions with attempt count and last error summary. Use show_failed_partition for full stack traces."),

            AIFunctionFactory.Create(
                (
                    [Description("Observer ID")] string observerId,
                    [Description("Partition key")] string partition,
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    FindFailedPartitionAsync(services, eventStoreName ?? eventStore, namespaceName ?? ns, observerId, partition),
                "show_failed_partition",
                "Show full details for a specific failed partition including stack traces (truncated to 500 chars per entry)."),

            // ── Recommendations ────────────────────────────────────────────────
            AIFunctionFactory.Create(
                (
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    SerializeAsync(
                        services.Recommendations.GetRecommendations(new GetRecommendationsRequest
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns
                        }),
                        r => new
                        {
                            id = r.Id,
                            name = r.Name,
                            type = r.Type,
                            description = r.Description,
                            occurred = r.Occurred
                        }),
                "list_recommendations",
                "List system recommendations (e.g. observer replays, index suggestions)."),

            // ── Projections & read models ──────────────────────────────────────
            AIFunctionFactory.Create(
                ([Description("Event store name (optional)")] string? eventStoreName = null) =>
                    SerializeAsync(
                        services.Projections.GetAllDefinitions(new GetAllDefinitionsRequest
                        {
                            EventStore = eventStoreName ?? eventStore
                        }),
                        p => new
                        {
                            identifier = p.Identifier,
                            readModel = p.ReadModel,
                            isActive = p.IsActive,
                            isRewindable = p.IsRewindable
                        }),
                "list_projections",
                "List projection definitions (identifier, read model, active/rewindable flags). Full definition with mappings is omitted to save tokens."),

            AIFunctionFactory.Create(
                ([Description("Event store name (optional)")] string? eventStoreName = null) =>
                    SerializeResponseAsync(async () =>
                    {
                        var response = await services.ReadModels.GetDefinitions(new GetDefinitionsRequest
                        {
                            EventStore = eventStoreName ?? eventStore
                        });

                        var items = response.ReadModels
                            .Take(MaxListItems)
                            .Select(rm => new
                            {
                                identifier = rm.Type.Identifier,
                                containerName = rm.ContainerName,
                                displayName = rm.DisplayName,
                                observerType = rm.ObserverType.ToString()
                            })
                            .ToList();

                        return BuildListResult(items, response.ReadModels.Count);
                    }),
                "list_read_models",
                "List read model definitions (identifier, container, display name). Schema omitted to save tokens."),

            AIFunctionFactory.Create(
                (
                    [Description("The read model type name")] string readModel,
                    [Description("The key to look up")] string key,
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    SerializeResponseAsync(() => services.ReadModels.GetInstanceByKey(new GetInstanceByKeyRequest
                    {
                        EventStore = eventStoreName ?? eventStore,
                        Namespace = namespaceName ?? ns,
                        ReadModelIdentifier = readModel,
                        ReadModelKey = key,
                        EventSequenceId = CliDefaults.DefaultEventSequenceId
                    })),
                "get_read_model",
                "Get a single read model instance by its key. Returns the full document."),

            // ── Identities, users, applications ───────────────────────────────
            AIFunctionFactory.Create(
                (
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    SerializeAsync(
                        services.Identities.GetIdentities(new GetIdentitiesRequest
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns
                        }),
                        id => new
                        {
                            subject = id.Subject,
                            name = id.Name,
                            userName = id.UserName
                        }),
                "list_identities",
                "List known identities (event causation sources)."),

            AIFunctionFactory.Create(
                ([Description("Unused")] string? unused = null) =>
                    SerializeAsync(
                        services.Users.GetAll(),
                        u => new
                        {
                            id = u.Id,
                            username = u.Username,
                            email = u.Email,
                            isActive = u.IsActive
                        }),
                "list_users",
                "List Chronicle users (id, username, email, active status)."),

            AIFunctionFactory.Create(
                ([Description("Unused")] string? unused = null) =>
                    SerializeAsync(
                        services.Applications.GetAll(),
                        a => new
                        {
                            id = a.Id,
                            clientId = a.ClientId,
                            isActive = a.IsActive
                        }),
                "list_applications",
                "List registered OAuth client applications."),

            // ── Event sequence ─────────────────────────────────────────────────
            AIFunctionFactory.Create(
                (
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    GetTailSequenceNumberAsync(services, eventStoreName ?? eventStore, namespaceName ?? ns),
                "get_event_sequence_tail",
                "Get the highest used event sequence number (a number, not a count — gaps may exist)."),

            // ── Write tools ────────────────────────────────────────────────────
            AIFunctionFactory.Create(
                (
                    [Description("The observer ID to replay")] string observerId,
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    ExecuteWriteAsync(
                        () => services.Observers.Replay(new Replay
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns,
                            ObserverId = observerId,
                            EventSequenceId = CliDefaults.DefaultEventSequenceId
                        }),
                        "replay_observer",
                        observerId),
                "replay_observer",
                "Replay an observer from the beginning. This re-processes all events."),

            AIFunctionFactory.Create(
                (
                    [Description("The observer ID")] string observerId,
                    [Description("The partition key to replay")] string partition,
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    ExecuteWriteAsync(
                        () => services.Observers.ReplayPartition(new ReplayPartitionContract
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns,
                            ObserverId = observerId,
                            Partition = partition,
                            EventSequenceId = CliDefaults.DefaultEventSequenceId
                        }),
                        "replay_partition",
                        $"{observerId}/{partition}"),
                "replay_partition",
                "Replay a specific partition of an observer."),

            AIFunctionFactory.Create(
                (
                    [Description("The observer ID")] string observerId,
                    [Description("The partition key to retry")] string partition,
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    ExecuteWriteAsync(
                        () => services.Observers.RetryPartition(new RetryPartitionContract
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns,
                            ObserverId = observerId,
                            Partition = partition,
                            EventSequenceId = CliDefaults.DefaultEventSequenceId
                        }),
                        "retry_partition",
                        $"{observerId}/{partition}"),
                "retry_partition",
                "Retry a failed partition."),

            AIFunctionFactory.Create(
                (
                    [Description("The recommendation ID to perform")] string recommendationId,
                    [Description("Event store name (optional)")] string? eventStoreName = null,
                    [Description("Namespace (optional)")] string? namespaceName = null) =>
                    ExecuteWriteAsync(
                        () => services.Recommendations.Perform(new Perform
                        {
                            EventStore = eventStoreName ?? eventStore,
                            Namespace = namespaceName ?? ns,
                            RecommendationId = Guid.Parse(recommendationId)
                        }),
                        "perform_recommendation",
                        recommendationId),
                "perform_recommendation",
                "Perform a system recommendation.")
        ];
    }

    static string ResolveEventStore()
    {
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        return !string.IsNullOrWhiteSpace(ctx.EventStore) ? ctx.EventStore : CliDefaults.DefaultEventStoreName;
    }

    static string ResolveNamespace()
    {
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        return !string.IsNullOrWhiteSpace(ctx.Namespace) ? ctx.Namespace : CliDefaults.DefaultNamespaceName;
    }

    static async Task<string> GetTailSequenceNumberAsync(IServices services, string eventStore, string namespaceName)
    {
        var response = await services.EventSequences.GetTailSequenceNumber(new GetTailSequenceNumberRequest
        {
            EventStore = eventStore,
            Namespace = namespaceName,
            EventSequenceId = CliDefaults.DefaultEventSequenceId
        });

        return response.SequenceNumber.ToString();
    }

    static async Task<string> FindObserverAsync(IServices services, string eventStore, string namespaceName, string observerId)
    {
        var observers = await services.Observers.GetObservers(new AllObserversRequest
        {
            EventStore = eventStore,
            Namespace = namespaceName
        });

        var observer = observers.FirstOrDefault(o => string.Equals(o.Id, observerId, StringComparison.OrdinalIgnoreCase));
        if (observer is null)
        {
            return JsonSerializer.Serialize(new { error = "not_found", observerId }, CliServiceClient.JsonSerializerOptions);
        }

        var result = new
        {
            id = observer.Id,
            eventSequenceId = observer.EventSequenceId,
            type = observer.Type.ToString(),
            owner = observer.Owner,
            state = observer.RunningState.ToString(),
            nextSequenceNumber = observer.NextEventSequenceNumber,
            lastHandledSequenceNumber = observer.LastHandledEventSequenceNumber,
            isSubscribed = observer.IsSubscribed,
            eventTypes = observer.EventTypes.Select(et => et.ToString()).ToList()
        };

        return JsonSerializer.Serialize(result, CliServiceClient.JsonSerializerOptions);
    }

    static async Task<string> FindFailedPartitionAsync(IServices services, string eventStore, string namespaceName, string observerId, string partition)
    {
        var partitions = await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
        {
            EventStore = eventStore,
            Namespace = namespaceName,
            ObserverId = observerId
        });

        var fp = partitions.FirstOrDefault(p =>
            string.Equals(p.Partition, partition, StringComparison.OrdinalIgnoreCase));

        if (fp is null)
        {
            return JsonSerializer.Serialize(new { error = "not_found", observerId, partition }, CliServiceClient.JsonSerializerOptions);
        }

        var result = new
        {
            id = fp.Id,
            observerId = fp.ObserverId,
            partition = fp.Partition,
            attemptCount = fp.Attempts.Count(),
            attempts = fp.Attempts.Select(a => new
            {
                occurred = a.Occurred,
                sequenceNumber = a.SequenceNumber,
                messages = a.Messages.Take(5).ToList(),
                stackTrace = TruncateMessage(a.StackTrace, 500)
            }).ToList()
        };

        return JsonSerializer.Serialize(result, CliServiceClient.JsonSerializerOptions);
    }

    static async Task<string> FindEventTypeAsync(IServices services, string eventStore, string eventTypeId)
    {
        var registrations = await services.EventTypes.GetAllRegistrations(new GetAllEventTypesRequest
        {
            EventStore = eventStore
        });

        var reg = registrations.FirstOrDefault(r =>
            string.Equals(r.Type.Id, eventTypeId, StringComparison.OrdinalIgnoreCase));

        if (reg is null)
        {
            return JsonSerializer.Serialize(new { error = "not_found", eventTypeId }, CliServiceClient.JsonSerializerOptions);
        }

        return JsonSerializer.Serialize(reg, CliServiceClient.JsonSerializerOptions);
    }

    static async Task<string> SerializeAsync<T>(Task<IEnumerable<T>> task)
    {
        var items = (await task).ToList();
        return JsonSerializer.Serialize(BuildListResult(items, items.Count), CliServiceClient.JsonSerializerOptions);
    }

    static async Task<string> SerializeAsync<T, TProjected>(Task<IEnumerable<T>> task, Func<T, TProjected> project)
    {
        var all = (await task).ToList();
        var projected = all.Take(MaxListItems).Select(project).ToList();
        return JsonSerializer.Serialize(BuildListResult(projected, all.Count), CliServiceClient.JsonSerializerOptions);
    }

    static async Task<string> SerializeAsync<T, TProjected>(Task<IList<T>> task, Func<T, TProjected> project)
    {
        var all = await task;
        var projected = all.Take(MaxListItems).Select(project).ToList();
        return JsonSerializer.Serialize(BuildListResult(projected, all.Count), CliServiceClient.JsonSerializerOptions);
    }

    static async Task<string> SerializeResponseAsync<T>(Func<Task<T>> call)
    {
        var result = await call();
        return JsonSerializer.Serialize(result, CliServiceClient.JsonSerializerOptions);
    }

    static async Task<string> ExecuteWriteAsync(Func<Task> action, string toolName, string target)
    {
        await action();
        return JsonSerializer.Serialize(new { status = "success", tool = toolName, target }, CliServiceClient.JsonSerializerOptions);
    }

    /// <summary>
    /// Wraps a projected list with count metadata so the AI knows if results were truncated.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="items">The (possibly truncated) items to include.</param>
    /// <param name="totalCount">The total count before truncation.</param>
    /// <returns>An anonymous object with count metadata and the items.</returns>
    static object BuildListResult<T>(IReadOnlyList<T> items, int totalCount)
    {
        if (totalCount <= MaxListItems)
        {
            return new { count = totalCount, items };
        }

        return new
        {
            count = totalCount,
            showing = items.Count,
            truncated = true,
            note = $"Results capped at {MaxListItems}. Use more specific filters to narrow results.",
            items
        };
    }

    static string TruncateMessage(string message, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
        {
            return message;
        }

        return $"{message[..maxLength]}…";
    }
}
