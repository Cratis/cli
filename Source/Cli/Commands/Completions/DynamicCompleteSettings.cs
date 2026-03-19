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
    /// Gets or sets the event store name.
    /// </summary>
    [CommandOption("-e|--event-store <NAME>")]
    [Description("Event store name")]
    [DefaultValue(CliDefaults.DefaultEventStoreName)]
    public string EventStore { get; set; } = CliDefaults.DefaultEventStoreName;

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    [CommandOption("-n|--namespace <NAME>")]
    [Description("Namespace within the event store")]
    [DefaultValue(CliDefaults.DefaultNamespaceName)]
    public string Namespace { get; set; } = CliDefaults.DefaultNamespaceName;

    /// <summary>
    /// Resolves the effective event store name by checking flag then default.
    /// </summary>
    /// <returns>The resolved event store name.</returns>
    public string ResolveEventStore()
    {
        if (EventStore != CliDefaults.DefaultEventStoreName)
        {
            return EventStore;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        return !string.IsNullOrWhiteSpace(ctx.EventStore) ? ctx.EventStore : CliDefaults.DefaultEventStoreName;
    }

    /// <summary>
    /// Resolves the effective namespace by checking flag then default.
    /// </summary>
    /// <returns>The resolved namespace name.</returns>
    public string ResolveNamespace()
    {
        if (Namespace != CliDefaults.DefaultNamespaceName)
        {
            return Namespace;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        return !string.IsNullOrWhiteSpace(ctx.Namespace) ? ctx.Namespace : CliDefaults.DefaultNamespaceName;
    }
}
