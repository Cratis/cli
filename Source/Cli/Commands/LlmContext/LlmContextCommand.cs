// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.LlmContext;

/// <summary>
/// Outputs a machine-readable description of all CLI capabilities for AI agents.
/// </summary>
[CliCommand("llm-context", "Output CLI capabilities as JSON for AI agent consumption", ExcludeFromLlm = true)]
[CliExample("llm-context")]
[LlmOutputAdvice("json", "Always outputs JSON regardless of --output flag.")]
public partial class LlmContextCommand : AsyncCommand<GlobalSettings>
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var descriptor = new LlmContextDescriptor
        {
            Tool = "cratis",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            Description = "CLI for managing Chronicle event-sourced systems. Connects to a Chronicle server over gRPC.",
            GlobalOptions =
            [
                new OptionDescriptor("--server", "string", "Chronicle server connection string (e.g. chronicle://localhost:35000)"),
                new OptionDescriptor("-o, --output", "string", "Output format: table (rich terminal table), plain (tab-separated), json (indented), or json-compact (compact JSON). Defaults to auto-detection: json-compact in AI environments, json when output is redirected, table in interactive terminals. Use -o plain for commands that return large payloads (events get, event-types list, projections list) — see per-command output guidance."),
                new OptionDescriptor("-q, --quiet", "bool", "Quiet mode: output only key identifiers, one per line. Suppresses messages and formatting. Ideal for piping into other commands."),
                new OptionDescriptor("-y, --yes", "bool", "Skip confirmation prompts (assume yes). Required for non-interactive usage of destructive commands (replay, retry, remove, etc.)."),
            ],
            CommandGroups = BuildDiscoveredCommandGroups(),
            ConnectionInfo = new ConnectionInfoDescriptor
            {
                DefaultConnectionString = "chronicle://<client>:<secret>@localhost:35000",
                EnvironmentVariable = CliDefaults.ConnectionStringEnvVar,
                ConfigFile = CliConfiguration.GetConfigPath(),
                Precedence = ["--server flag", "CHRONICLE_CONNECTION_STRING env var", "config file", "default (localhost:35000)"],
            },
            Tips =
            [
                "Default output in AI environments is json-compact (named fields, no whitespace). Use -o plain only for commands that return large payloads: event-types list (~34x smaller), events get (~25x smaller), read-models list (~27x smaller), projections list (JSON includes full schemas and definitions). For all other commands, json-compact is fine.",
                "Use -o json or -o json-compact for show/detail commands where you need nested structure: observers show, projections show, failed-partitions show, read-models get, config show, auth status. See per-command output guidance below for the full list.",
                "Enums in JSON output serialize as human-readable names (e.g. 'Client', 'Projection') rather than integers.",
                "Pipe plain output through grep/awk for filtering; use --output json with jq only when structured parsing is essential.",
                "Set a default server with: cratis config set server chronicle://myhost:35000",
                "Most commands require --event-store and --namespace; both default to 'default'.",
                "Use 'cratis observers list --type reactor' to filter by observer type.",
                "config path outputs the same format regardless of --output flag.",
                "Use 'cratis version -o json' to check CLI/server contract compatibility programmatically.",
                "Use 'cratis update' to update the CLI to the latest version without remembering the NuGet package name.",
                "Use --quiet (-q) to get only IDs from list commands — ideal for piping: cratis observers list -q | xargs -I {} cratis observers replay {} -y",
                "Use --yes (-y) to skip confirmation prompts in scripts and automation. Destructive commands (replay, retry, remove) prompt for confirmation in interactive terminals.",
                "JSON errors include a machine-parseable 'error' code (e.g. 'not_found', 'connection_error', 'server_error', 'authentication_error', 'validation_error') alongside the human-readable 'message' field.",
                "Use 'cratis init' to generate a CHRONICLE.md reference document and configure AI tools (Claude Code, GitHub Copilot, Cursor, Windsurf) for your project.",
                "Use 'cratis completions bash|zsh|fish' to generate shell completion scripts for tab-completion support.",

                // Chat command temporarily disabled — re-enable for future release:
                // "Use 'cratis chat' for an interactive AI assistant that can query and operate on your Chronicle system.",
                // "Use 'cratis chat \"your question\"' for single-question mode — the AI answers and exits without entering the REPL.",
            ],
            OutputFormatGuidance = new OutputFormatGuidanceDescriptor
            {
                Summary = "Default in AI environments is json-compact (compact named-field JSON, no whitespace). Use -o plain for commands that return large payloads where you only need key columns (event-types list, events get, read-models list, projections list — these are 25-34x smaller in plain). Use json-compact or json for detail/show commands where nested structure matters.",
                PerCommand = BuildDiscoveredOutputAdvice(),
            },
        };

        Console.WriteLine(JsonSerializer.Serialize(descriptor, _serializerOptions));
        return Task.FromResult(ExitCodes.Success);
    }

    static IReadOnlyList<OptionDescriptor> EventStoreOptions() =>
    [
        new("-e, --event-store", "string", "Event store name (default: default)"),
        new("-n, --namespace", "string", "Namespace within the event store (default: default)"),
    ];
}
