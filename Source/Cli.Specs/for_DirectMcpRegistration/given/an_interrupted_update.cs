// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.given;

/// <summary>
/// Claude Code and Cursor were registered, then an update to a pinned tenant wrote Claude Code's configuration and was
/// refused Cursor's, because another process changed that file while the update ran.
/// </summary>
public class an_interrupted_update : a_home_and_a_project
{
    protected const string Tenant = "team";
    protected Exception _interruption;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude", "cursor"]);
        var update = DirectMcpRegistration.Install(DirectMcpScope.User, Locations, ["claude", "cursor"], DirectMcpClients.Arguments(Origin, Tenant));
        var cursor = ReadJson(HomeFile(".cursor/mcp.json"));
        cursor["mcpServers"]!["other"] = new System.Text.Json.Nodes.JsonObject { ["command"] = "other" };
        File.WriteAllText(HomeFile(".cursor/mcp.json"), cursor.ToJsonString());
        _interruption = Catch.Exception(() => update.Apply(new(false)));
    }

    protected static string Launched(string path, string collection) => ReadJson(path)[collection]![DirectMcpClients.Id]!.ToJsonString();
}
