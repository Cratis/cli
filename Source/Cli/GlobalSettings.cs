// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Global settings shared by all CLI commands.
/// </summary>
public class GlobalSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the output format.
    /// </summary>
    [CommandOption("-o|--output <FORMAT>")]
    [Description("Output format: table (rich terminal), plain (tab-separated), json, or json-compact (compact JSON)")]
    [DefaultValue(OutputFormats.Auto)]
    public string Output { get; set; } = OutputFormats.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether quiet mode is enabled.
    /// When enabled, only key identifiers are emitted, one per line, with no headers or decoration.
    /// </summary>
    [CommandOption("-q|--quiet")]
    [Description("Quiet mode: output only key identifiers, one per line. Suppresses messages and formatting.")]
    [DefaultValue(false)]
    public bool Quiet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether confirmation prompts should be skipped.
    /// </summary>
    [CommandOption("-y|--yes")]
    [Description("Skip confirmation prompts (assume yes)")]
    [DefaultValue(false)]
    public bool Yes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether debug output should be written to stderr.
    /// When enabled, prints resolved config path, context, connection string (credentials redacted), and RPC timing.
    /// </summary>
    [CommandOption("--debug")]
    [Description("Print debug information to stderr: resolved config path, context, connection string, and RPC timing")]
    [DefaultValue(false)]
    public bool Debug { get; set; }

    /// <summary>
    /// Returns true when the process is running inside a known AI agent environment (Claude Code, Cursor, Windsurf).
    /// Used to tune default output formats — compact JSON rather than indented JSON.
    /// </summary>
    /// <returns>True if an AI agent environment is detected.</returns>
    public static bool IsAiAgentEnvironment() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLAUDECODE")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLAUDE_CODE_ENTRYPOINT")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CURSOR_TRACE_DIR")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINDSURF_SESSION_ID")) ||
        string.Equals(Environment.GetEnvironmentVariable("TERM_PROGRAM"), "cursor", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("TERM_PROGRAM"), "windsurf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the effective output format, using auto-detection when set to "auto".
    /// </summary>
    /// <returns>The resolved output format name.</returns>
    public string ResolveOutputFormat()
    {
        if (Quiet)
        {
            // When an explicit JSON format is also requested, output JSON array of identifiers.
            if (string.Equals(Output, OutputFormats.Json, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Output, OutputFormats.JsonCompact, StringComparison.OrdinalIgnoreCase))
            {
                return OutputFormats.JsonQuiet;
            }
            return OutputFormats.Quiet;
        }

        if (string.Equals(Output, OutputFormats.JsonCompact, StringComparison.OrdinalIgnoreCase))
        {
            return OutputFormats.JsonCompact;
        }

        if (!string.Equals(Output, OutputFormats.Auto, StringComparison.OrdinalIgnoreCase))
        {
            return Output.ToLowerInvariant();
        }

        // When running inside an AI agent environment, default to compact JSON.
        // LLMs are trained on JSON and handle named fields reliably; plain (tab-separated) loses
        // nested structure and requires column-position memory. For commands where plain is dramatically
        // smaller (events get, event-types list, projections list), the LlmContext output guidance
        // instructs the AI to add -o plain explicitly — keeping that opt-in rather than the default.
        if (IsAiAgentEnvironment())
        {
            return OutputFormats.JsonCompact;
        }

        return Console.IsOutputRedirected ? OutputFormats.Json : OutputFormats.Table;
    }
}
