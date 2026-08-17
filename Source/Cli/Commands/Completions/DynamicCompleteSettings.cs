// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Settings for the dynamic completion command.
/// </summary>
public class DynamicCompleteSettings : ChronicleSettings
{
    /// <summary>
    /// Gets or sets the resource context to complete (e.g. "observers", "jobs", "read-models").
    /// </summary>
    [CommandArgument(0, "<CONTEXT>")]
    [Description("Resource context to complete identifiers for")]
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event store name. <see langword="null"/> when the option was not passed explicitly.
    /// </summary>
    [CommandOption("-e|--event-store <NAME>")]
    [Description("Event store name (defaults to the context's event store, then 'default')")]
    public string? EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace name. <see langword="null"/> when the option was not passed explicitly.
    /// </summary>
    [CommandOption("-n|--namespace <NAME>")]
    [Description("Namespace within the event store (defaults to the context's namespace, then 'Default')")]
    public string? Namespace { get; set; }

    /// <summary>
    /// Resolves the effective event store name by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved event store name.</returns>
    public string ResolveEventStore() => EventStoreResolution.ResolveEventStore(EventStore).Value;

    /// <summary>
    /// Resolves the effective namespace by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved namespace name.</returns>
    public string ResolveNamespace() => EventStoreResolution.ResolveNamespace(Namespace).Value;
}
