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
[CliExample("llm-context")]
[LlmOutputAdvice("json", "Always outputs JSON regardless of --output flag.")]
public partial class LlmContextCommand : AsyncCommand<LlmContextSettings>
{
#pragma warning disable MA0136
    internal const string JsonSchema = /*lang=json,strict*/ """
        {
          "$schema": "https://json-schema.org/draft-07/schema",
          "title": "cratis llm-context",
          "description": "Machine-readable capability descriptor emitted by 'cratis llm-context'.",
          "type": "object",
          "properties": {
            "tool": { "type": "string" },
            "version": { "type": "string" },
            "description": { "type": "string" },
            "globalOptions": { "type": "array", "items": { "$ref": "#/$defs/option" } },
            "commandGroups": { "type": "array", "items": { "$ref": "#/$defs/commandGroup" } },
            "connectionInfo": { "$ref": "#/$defs/connectionInfo" },
            "tips": { "type": "array", "items": { "type": "string" } },
            "outputFormatGuidance": { "$ref": "#/$defs/outputFormatGuidance" }
          },
          "$defs": {
            "option": {
              "type": "object",
              "properties": {
                "name": { "type": "string", "description": "Option name(s), e.g. '-e, --event-store' or '<EVENT_SOURCE_ID>'" },
                "type": { "type": "string", "description": "Value type: string, bool, int, enum" },
                "description": { "type": "string" }
              },
              "required": ["name", "type", "description"]
            },
            "command": {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "description": { "type": "string" },
                "inheritedOptions": { "type": "array", "items": { "$ref": "#/$defs/option" }, "description": "Options inherited from parent groups. Absent when hoisted to the group level." },
                "arguments": { "type": "array", "items": { "$ref": "#/$defs/option" }, "description": "Positional arguments named with angle brackets, e.g. <EVENT_SOURCE_ID>." },
                "options": { "type": "array", "items": { "$ref": "#/$defs/option" }, "description": "Named flags starting with '-'." }
              },
              "required": ["name", "description"]
            },
            "commandGroup": {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "description": { "type": "string" },
                "inheritedOptions": { "type": "array", "items": { "$ref": "#/$defs/option" }, "description": "Options shared by all commands in this group, hoisted here to avoid repetition." },
                "commands": { "type": "array", "items": { "$ref": "#/$defs/command" } },
                "subGroups": { "type": "array", "items": { "$ref": "#/$defs/commandGroup" } }
              },
              "required": ["name", "description"]
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
            "outputFormatGuidance": {
              "type": "object",
              "properties": {
                "summary": { "type": "string" },
                "perCommand": { "type": "array", "items": { "$ref": "#/$defs/commandOutputAdvice" } }
              }
            },
            "commandOutputAdvice": {
              "type": "object",
              "properties": {
                "command": { "type": "string" },
                "format": { "type": "string" },
                "reason": { "type": "string" }
              }
            }
          }
        }
        """;
#pragma warning restore MA0136

    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Builds the LLM context descriptor and serializes it to a JSON string.
    /// </summary>
    /// <returns>The JSON string.</returns>
    internal static string BuildDescriptorJson() =>
        JsonSerializer.Serialize(BuildDescriptor(), SerializerOptions);

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(CommandContext context, LlmContextSettings settings, CancellationToken cancellationToken)
    {
        Console.WriteLine(settings.Schema ? JsonSchema : BuildDescriptorJson());
        return Task.FromResult(ExitCodes.Success);
    }

    static LlmContextDescriptor BuildDescriptor() => new()
    {
        Tool = "cratis",
        Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0",
        Description = "CLI for managing Chronicle event-sourced systems. Connects to a Chronicle server over gRPC.",
        GlobalOptions =
        [
            new OptionDescriptor("--server", "string", "Chronicle server connection string (e.g. chronicle://localhost:35000)"),
            new OptionDescriptor("-o, --output", "string", "Output format: table (rich terminal table), plain (tab-separated), json (indented), or json-compact (compact JSON). Current auto-detection uses table in interactive terminals, json for redirected output, plain with NO_COLOR, and json-compact for recognized tool-environment markers. Select a format explicitly for automation."),
            new OptionDescriptor("-q, --quiet", "bool", "Quiet mode: output only key identifiers, one per line. Use for bounded selection or inspection; review scope before passing an identifier to a state-changing command."),
            new OptionDescriptor("-y, --yes", "bool", "Skip confirmation prompts (assume yes). Required for non-interactive state-changing commands; use only with a bounded target, authorization, current state, and recovery procedure."),
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
            "A recognized tool-environment marker may select json-compact. Use -o explicitly for automation. Plain output is tab-separated; output size depends on the command, version, and returned data.",
            "Use -o json or -o json-compact for show/detail commands where you need nested structure: observers show, projections show, failed-partitions show, read-models get, config show, auth status. See per-command output guidance for the full list.",
            "Enums in JSON output serialize as human-readable names (e.g. 'Client', 'Projection') rather than integers.",
            "Pipe plain output through grep/awk for filtering; use --output json with jq only when structured parsing is essential.",
            "Set a default server with: cratis context set-value server chronicle://myhost:35000",
            "Most chronicle commands require --event-store and --namespace; when omitted they fall back to the active context's values, then to 'default' / 'Default'. An explicitly passed value always wins, even when it equals the built-in default name. Groups that require them declare inheritedOptions at the group level — do not re-add those options to the command arguments.",
            "Use 'cratis chronicle observers list --type reactor' to filter by observer type.",
            "Use 'cratis version -o json' to check CLI/server contract compatibility programmatically.",
            "Use 'cratis update' to update the CLI to the latest version without remembering the NuGet package name.",
            "Use --quiet (-q) to get identifiers for bounded selection or inspection: cratis chronicle observers list -q | head -n 5. Confirm scope before a state-changing command.",
            "Use --yes (-y) only after the exact state-changing target, authorization, current state, and recovery procedure are bounded. Replay, retry, and remove prompt in interactive terminals.",
            "JSON errors include a machine-parseable 'error' code (e.g. 'not_found', 'connection_error', 'server_error', 'authentication_error', 'validation_error') alongside the human-readable 'message' field.",
            "Use 'cratis init' to generate a CHRONICLE.md reference document and the current configured tool-context files for your project.",
            "Use 'cratis completions bash|zsh|fish' to generate shell completion scripts for tab-completion support.",
            "Run 'cratis llm-context --schema' to get the JSON Schema for this output format.",
        ],
        OutputFormatGuidance = new OutputFormatGuidanceDescriptor
        {
            Summary = "A recognized tool-environment marker may select json-compact. Use -o explicitly for automation. Plain is tab-separated; json and json-compact retain the current named structure. Output size depends on the command, version, and returned data.",
            PerCommand = BuildDiscoveredOutputAdvice(),
        },
    };

    static IReadOnlyList<OptionDescriptor> EventStoreOptions() =>
    [
        new("-e, --event-store", "string", "Event store name (defaults to the context's event store, then 'default')"),
        new("-n, --namespace", "string", "Namespace within the event store (defaults to the context's namespace, then 'Default')"),
    ];
}
