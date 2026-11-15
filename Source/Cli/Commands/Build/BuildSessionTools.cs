// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The MCP servers a host configures on every agent session: Screenplay's tools on the order's model, served by this CLI,
/// and Studio's board tools when the organization's MCP credential is configured.
/// </summary>
public static class BuildSessionTools
{
    /// <summary>The name of the Screenplay server in a session.</summary>
    public const string Screenplay = "screenplay";

    /// <summary>The name of Studio's server in a session.</summary>
    public const string Studio = "studio";

    /// <summary>
    /// The Studio tools that only read, usable before a person confirmed a modeling proposal.
    /// </summary>
    public static readonly IReadOnlyList<string> StudioReadOnlyTools =
    [
        "studio_describe_command", "studio_describe_query", "studio_export_context", "studio_find_commands", "studio_find_queries",
        "studio_get_board_findings", "studio_get_brainstorming_board", "studio_get_event_model", "studio_get_structure", "studio_list_areas",
        "studio_list_brainstorming_boards", "studio_list_concepts", "studio_list_constraint_sticky_notes", "studio_list_constraints",
        "studio_list_event_models", "studio_list_event_source_sticky_notes", "studio_list_given_context_sticky_notes",
        "studio_list_persona_sticky_notes", "studio_list_seed_data_sticky_notes", "studio_list_state_machines", "studio_list_workflows",
        "studio_locate", "studio_perform_query"
    ];

    /// <summary>
    /// Describes the servers of a session.
    /// </summary>
    /// <param name="modelRoot">The folder holding the Screenplay documents, or <see langword="null"/> to leave Screenplay's tools out.</param>
    /// <param name="studioMcp">The address of Studio's MCP server, or <see langword="null"/>.</param>
    /// <param name="studioToken">The bearer token for Studio's MCP server, or <see langword="null"/>.</param>
    /// <returns>The servers.</returns>
    public static IReadOnlyList<BuildAgentTool> For(string? modelRoot, string? studioMcp, string? studioToken)
    {
        var tools = new List<BuildAgentTool>();
        if (modelRoot is not null)
        {
            var (program, arguments) = CurrentCli();
            tools.Add(new(Screenplay, program, [.. arguments, "screenplay", "mcp", modelRoot], null, null, []));
        }

        if (studioMcp is not null && studioToken is not null)
        {
            tools.Add(new(Studio, null, [], $"{studioMcp.TrimEnd('/')}/mcp", studioToken, StudioReadOnlyTools));
        }

        return tools;
    }

    /// <summary>
    /// Gets how to start this CLI again: its own executable, or the .NET host with its assembly when it runs as a tool.
    /// </summary>
    /// <returns>The program and the arguments that come before a command.</returns>
    public static (string Program, IReadOnlyList<string> Arguments) CurrentCli()
    {
        var process = Environment.ProcessPath ?? "cratis";
        var name = Path.GetFileNameWithoutExtension(process);
        if (!string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase)) return (process, []);
        var assembly = typeof(BuildSessionTools).Assembly.Location;
        return assembly.Length > 0 ? (process, [assembly]) : ("cratis", []);
    }
}
