// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.LlmContext;

/// <summary>
/// Outputs a machine-readable description of all CLI capabilities for AI agents.
/// </summary>
[CliCommand("llm-context", "Output CLI capabilities as JSON for AI agent consumption", ExcludeFromLlm = true)]
[LlmOutputAdvice("json", "Always outputs JSON regardless of --output flag. Use --schema to get the JSON Schema for this output.")]
public partial class LlmContextCommand : AsyncCommand<LlmContextSettings>
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

#pragma warning disable MA0136, RCS1187, CA1802, RCS1181 // Raw string literal for JSON schema; cannot be const
    static readonly string _schema =
        /*lang=json,strict*/
                             """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "title": "LlmContextDescriptor",
          "description": "Describes the full CLI surface returned by 'cratis llm-context'. Use this schema to understand how to parse the output before calling the command.",
          "type": "object",
          "required": ["tool", "version", "description", "globalOptions", "commandGroups", "connectionInfo", "tips", "outputFormatGuidance"],
          "properties": {
            "tool": { "type": "string", "description": "Always 'cratis'." },
            "version": { "type": "string", "description": "CLI assembly version." },
            "description": { "type": "string" },
            "globalOptions": {
              "type": "array",
              "description": "Options available on every command.",
              "items": { "$ref": "#/$defs/OptionDescriptor" }
            },
            "commandGroups": {
              "type": "array",
              "description": "Top-level command groups. Each group may contain direct commands and/or nested sub-groups.",
              "items": { "$ref": "#/$defs/CommandGroupDescriptor" }
            },
            "connectionInfo": {
              "type": "object",
              "properties": {
                "defaultConnectionString": { "type": "string" },
                "environmentVariable": { "type": "string" },
                "configFile": { "type": "string" },
                "precedence": { "type": "array", "items": { "type": "string" } }
              }
            },
            "tips": { "type": "array", "items": { "type": "string" } },
            "outputFormatGuidance": {
              "type": "object",
              "properties": {
                "summary": { "type": "string" },
                "perCommand": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "command": { "type": "string" },
                      "recommendedFormat": { "type": "string" },
                      "reason": { "type": "string" }
                    }
                  }
                }
              }
            }
          },
          "$defs": {
            "OptionDescriptor": {
              "type": "object",
              "required": ["name", "type", "description"],
              "properties": {
                "name": { "type": "string", "description": "Flag or positional argument name (e.g. '-e, --event-store' or '<ID>')." },
                "type": { "type": "string", "description": "Value type: string, bool, int, guid, uint." },
                "description": { "type": "string" }
              }
            },
            "CommandDescriptor": {
              "type": "object",
              "required": ["name", "description"],
              "properties": {
                "name": { "type": "string", "description": "Leaf command name (e.g. 'list', 'show')." },
                "description": { "type": "string" },
                "inheritedOptions": {
                  "type": ["array", "null"],
                  "description": "Options inherited from the parent group. Null when the parent group already declares them via its own inheritedOptions.",
                  "items": { "$ref": "#/$defs/OptionDescriptor" }
                },
                "arguments": {
                  "type": ["array", "null"],
                  "description": "Positional arguments in order (e.g. <OBSERVER_ID>). Must be supplied positionally without a flag name.",
                  "items": { "$ref": "#/$defs/OptionDescriptor" }
                },
                "options": {
                  "type": ["array", "null"],
                  "description": "Named flags and options (e.g. --type, -o). Optional unless the description says otherwise.",
                  "items": { "$ref": "#/$defs/OptionDescriptor" }
                }
              }
            },
            "CommandGroupDescriptor": {
              "type": "object",
              "required": ["name", "description"],
              "properties": {
                "name": { "type": "string", "description": "Branch name (e.g. 'chronicle', 'observers')." },
                "description": { "type": "string" },
                "inheritedOptions": {
                  "type": ["array", "null"],
                  "description": "Options shared by all commands in this group. When present, individual command objects omit these options to avoid repetition.",
                  "items": { "$ref": "#/$defs/OptionDescriptor" }
                },
                "commands": {
                  "type": ["array", "null"],
                  "description": "Leaf commands directly in this group.",
                  "items": { "$ref": "#/$defs/CommandDescriptor" }
                },
                "subGroups": {
                  "type": ["array", "null"],
                  "description": "Nested command groups (sub-branches) within this group.",
                  "items": { "$ref": "#/$defs/CommandGroupDescriptor" }
                }
              }
            }
          }
        }
        """;
#pragma warning restore MA0136, RCS1187, CA1802, RCS1181

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, LlmContextSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Schema)
        {
            Console.WriteLine(_schema);
            return Task.FromResult(ExitCodes.Success);
        }

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
                "Use -o json or -o json-compact for show/detail commands where you need nested structure: observers show, projections show, failed-partitions show, read-models get, auth status. See per-command output guidance for the full list.",
                "Enums in JSON output serialize as human-readable names (e.g. 'Client', 'Projection') rather than integers.",
                "Pipe plain output through grep/awk for filtering; use --output json with jq only when structured parsing is essential.",
                "Set a default server with: cratis context set-value server chronicle://myhost:35000",
                "Most chronicle commands require --event-store and --namespace; both default to 'default'. Groups that require them declare inheritedOptions at the group level — do not re-add those options to the command arguments.",
                "Use 'cratis chronicle observers list --type reactor' to filter by observer type.",
                "Use 'cratis version -o json' to check CLI/server contract compatibility programmatically.",
                "Use 'cratis update' to update the CLI to the latest version without remembering the NuGet package name.",
                "Use --quiet (-q) to get only IDs from list commands — ideal for piping: cratis chronicle observers list -q | xargs -I {} cratis chronicle observers replay {} -y",
                "Use --yes (-y) to skip confirmation prompts in scripts and automation. Destructive commands (replay, retry, remove) prompt for confirmation in interactive terminals.",
                "JSON errors include a machine-parseable 'error' code (e.g. 'not_found', 'connection_error', 'server_error', 'authentication_error', 'validation_error') alongside the human-readable 'message' field.",
                "Use 'cratis init' to generate a CHRONICLE.md reference document and configure AI tools (Claude Code, GitHub Copilot, Cursor, Windsurf) for your project.",
                "Use 'cratis completions bash|zsh|fish' to generate shell completion scripts for tab-completion support.",
                "Run 'cratis llm-context --schema' to get the JSON Schema for this output format.",
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
