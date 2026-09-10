// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Registration;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Settings for commands that operate within a specific event store and namespace.
/// </summary>
public class EventStoreSettings : ChronicleSettings
{
    /// <summary>
    /// Gets or sets the event store name. <see langword="null"/> when the option was not passed explicitly.
    /// </summary>
    [CommandOption("-e|--event-store <NAME>")]
    [DynamicOptionCompletion("event-stores")]
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
    public string ResolveEventStore() => ResolveEventStoreWithSource().Value;

    /// <summary>
    /// Resolves the effective event store name and the source it came from, by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved event store name and its <see cref="SettingSource"/>.</returns>
    public ResolvedSetting ResolveEventStoreWithSource() => EventStoreResolution.ResolveEventStore(EventStore);

    /// <summary>
    /// Resolves the effective namespace by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved namespace name.</returns>
    public string ResolveNamespace() => ResolveNamespaceWithSource().Value;

    /// <summary>
    /// Resolves the effective namespace and the source it came from, by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved namespace name and its <see cref="SettingSource"/>.</returns>
    public ResolvedSetting ResolveNamespaceWithSource() => EventStoreResolution.ResolveNamespace(Namespace);
}
