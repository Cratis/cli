// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Direct;

/// <summary>Conservative, local-only detection; explicit selections never install additional clients.</summary>
internal static class DirectSetupClients
{
    internal static IReadOnlyList<string> Select(string[] requested, DirectMcpLocations locations)
    {
        if (requested.Length != 0)
        {
            var clients = requested.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Select(value => value.ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToArray();
            if (clients.Length == 0) throw new DirectAuthError("--client requires at least one client name.");
            foreach (var client in clients)
            {
                if (!DirectMcpClients.All.Contains(client, StringComparer.Ordinal)) throw new DirectAuthError($"Unknown MCP client '{client}'; choose one of {string.Join(", ", DirectMcpClients.All)}.");
            }
            return clients;
        }
        return [.. DirectMcpClients.All.Where(client => Detected(client, locations))];
    }

    static bool Detected(string client, DirectMcpLocations locations)
    {
        var relative = client switch
        {
            "claude" => ".claude",
            "codex" => ".codex",
            "cursor" => ".cursor",
            "copilot" => ".copilot",
            "pi" => ".pi/agent",
            _ => string.Empty
        };
        if (relative.Length != 0 && Directory.Exists(Path.Combine(locations.Home, relative))) return true;
        if (client == "copilot" && Directory.Exists(Path.Combine(locations.Project, ".vscode"))) return true;
        if (client == "pi" && !string.IsNullOrWhiteSpace(locations.Environment("PI_CODING_AGENT_DIR"))) return true;
        return DirectMcpClients.IsPresent(client, DirectMcpScope.User, locations) || DirectMcpClients.IsPresent(client, DirectMcpScope.Project, locations);
    }
}
