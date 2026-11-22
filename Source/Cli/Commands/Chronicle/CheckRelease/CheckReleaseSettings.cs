// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// Settings for the check-release command.
/// </summary>
/// <remarks>
/// Deliberately not <see cref="EventStoreSettings"/>: the event store is resolved without the interactive store selection, which could prompt or write the configuration.
/// </remarks>
public class CheckReleaseSettings : ChronicleSettings
{
    /// <summary>
    /// Gets or sets the path of the release description file.
    /// </summary>
    [CommandOption("--release <FILE>")]
    [Description("Path of the release description JSON file")]
    public string? Release { get; set; }

    /// <summary>
    /// Gets or sets the event store name. <see langword="null"/> when the option was not passed explicitly.
    /// </summary>
    [CommandOption("-e|--event-store <NAME>")]
    [DynamicOptionCompletion("event-stores")]
    [Description("Event store to check the registrations against (defaults to the context's event store, then 'default')")]
    public string? EventStore { get; set; }

    /// <summary>
    /// Resolves the effective event store name by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved event store name.</returns>
    public string ResolveEventStore() => EventStoreResolution.ResolveEventStore(EventStore).Value;
}
