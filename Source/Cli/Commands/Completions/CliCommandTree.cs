// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Provides the static command tree for the Cratis CLI, used by shell completion generators.
/// This must be kept in sync with registrations in <see cref="CliApp"/>.
/// </summary>
public static class CliCommandTree
{
    /// <summary>
    /// Global options available on all commands.
    /// </summary>
    public static readonly IReadOnlyList<string> GlobalOptions =
    [
        "--server",
        "-o", "--output",
        "-q", "--quiet",
        "-y", "--yes",
        "--management-port",
        "--debug"
    ];

    static readonly IReadOnlyList<string> _eventStoreOptions = ["-e", "--event-store", "-n", "--namespace"];
    static readonly IReadOnlyList<string> _eventStoreWithSequenceOptions = ["-e", "--event-store", "-n", "--namespace", "--sequence"];

    /// <summary>
    /// Gets the full command tree.
    /// </summary>
    public static IReadOnlyList<CommandNode> Commands =>
    [
        new(
            "chronicle",
            "Commands for interacting with Cratis Chronicle",
            [],
            [
                new(
                    "event-stores",
                    "Manage event stores",
                    [],
                    [
                        new("list", "List all event stores")
                    ]),
                new(
                    "namespaces",
                    "Manage namespaces within an event store",
                    [],
                    [
                        new("list", "List namespaces in an event store", _eventStoreOptions)
                    ]),
                new(
                    "event-types",
                    "Manage event types",
                    [],
                    [
                        new("list", "List registered event types", _eventStoreOptions),
                        new("show", "Show an event type registration with its JSON schema", _eventStoreOptions)
                    ]),
                new(
                    "events",
                    "Query and inspect events",
                    [],
                    [
                        new(
                            "get",
                            "Get events from an event sequence",
                            ["-e", "--event-store", "-n", "--namespace", "--sequence", "--from", "--to", "--event-source-id", "--event-type"]),
                        new("event", "Get a specific event by sequence number", ["-e", "--event-store", "-n", "--namespace", "--sequence"]),
                        new(
                            "tail",
                            "Get the highest used sequence number",
                            ["-e", "--event-store", "-n", "--namespace", "--sequence", "--event-type", "--event-source-id"]),
                        new("has", "Check if events exist for an event source ID", ["-e", "--event-store", "-n", "--namespace", "--sequence"])
                    ]),
                new(
                    "observers",
                    "Manage observers",
                    [],
                    [
                        new("list", "List observers", ["-e", "--event-store", "-n", "--namespace", "--type"]),
                        new("show", "Show detailed information about an observer", _eventStoreWithSequenceOptions),
                        new("replay", "Replay an observer from the beginning", _eventStoreWithSequenceOptions),
                        new("replay-partition", "Replay a specific partition of an observer", _eventStoreWithSequenceOptions),
                        new("retry-partition", "Retry a failed partition", _eventStoreWithSequenceOptions)
                    ]),
                new(
                    "failed-partitions",
                    "Inspect failed observer partitions",
                    [],
                    [
                        new("list", "List failed partitions", ["-e", "--event-store", "-n", "--namespace", "--observer"]),
                        new("show", "Show detailed information about a failed partition", _eventStoreOptions)
                    ]),
                new(
                    "recommendations",
                    "Manage system recommendations",
                    [],
                    [
                        new("list", "List recommendations", _eventStoreOptions),
                        new("perform", "Perform a recommendation", _eventStoreOptions),
                        new("ignore", "Ignore a recommendation", _eventStoreOptions)
                    ]),
                new(
                    "jobs",
                    "Manage background jobs",
                    [],
                    [
                        new("list", "List all jobs", _eventStoreOptions),
                        new("get", "Show detailed information about a specific job", _eventStoreOptions),
                        new("stop", "Stop a running job", _eventStoreOptions),
                        new("resume", "Resume a stopped or failed job", _eventStoreOptions)
                    ]),
                new(
                    "identities",
                    "Inspect identities",
                    [],
                    [
                        new("list", "List known identities", _eventStoreOptions)
                    ]),
                new(
                    "projections",
                    "Manage projections",
                    [],
                    [
                        new("list", "List projection definitions", _eventStoreOptions),
                        new("show", "Show a projection declaration", _eventStoreOptions)
                    ]),
                new(
                    "read-models",
                    "Inspect read model data",
                    [],
                    [
                        new("list", "List read model definitions", _eventStoreOptions),
                        new("instances", "List read model instances", ["-e", "--event-store", "-n", "--namespace", "--page", "--page-size"]),
                        new("get", "Get a single read model instance by key", _eventStoreWithSequenceOptions),
                        new("occurrences", "List read model occurrences", ["-e", "--event-store", "-n", "--namespace", "--generation"]),
                        new("snapshots", "Get snapshots for a read model instance", _eventStoreWithSequenceOptions)
                    ]),
                new(
                    "auth",
                    "Authentication management",
                    [],
                    [
                        new("status", "Show current authentication status")
                    ]),
                new("diagnose", "Run a health check and show a diagnostic report", ["-e", "--event-store", "-n", "--namespace", "--watch", "--interval"]),
                new("login", "Log in as a user", ["--password"]),
                new("logout", "Clear the cached login session"),
                new(
                    "users",
                    "Manage Chronicle users",
                    [],
                    [
                        new("list", "List all users", _eventStoreOptions),
                        new("add", "Add a new user", _eventStoreOptions),
                        new("remove", "Remove a user", _eventStoreOptions)
                    ]),
                new(
                    "applications",
                    "Manage OAuth client applications",
                    [],
                    [
                        new("list", "List all applications", _eventStoreOptions),
                        new("add", "Add a new application", _eventStoreOptions),
                        new("remove", "Remove an application", _eventStoreOptions),
                        new("rotate-secret", "Rotate an application's client secret", _eventStoreOptions)
                    ])
            ]),
        new(
            "context",
            "Manage named connection contexts",
            [],
            [
                new("list", "List all contexts"),
                new("create", "Create a new context", ["--server", "-e", "--event-store", "-n", "--namespace"]),
                new("set", "Switch to a context"),
                new("show", "Show current context details"),
                new("delete", "Delete a context"),
                new("rename", "Rename a context")
            ]),
        new(
            "config",
            "Manage CLI configuration",
            [],
            [
                new("show", "Show current configuration"),
                new("set", "Set a configuration value"),
                new("path", "Print configuration file path")
            ]),
        new("llm-context", "Output CLI capabilities as JSON for AI agents"),
        new("version", "Show CLI and server version information"),
        new("update", "Update the Cratis CLI to the latest version", ["--version"]),
        new("init", "Generate CHRONICLE.md and configure AI tools", ["--force", "--tool", "--no-commands"]),
        new("completions", "Generate shell completion scripts"),
        new("chat", "Chat with an AI assistant about your Chronicle system", ["--provider", "--model", "--no-tools", "--system"])
    ];
}
