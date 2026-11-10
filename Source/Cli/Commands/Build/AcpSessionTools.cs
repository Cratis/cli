// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Describes a session's MCP servers the way <c language="csharp">session/new</c> takes them. Credentials go here, in the session setup, and
/// never into a prompt.
/// </summary>
public static class AcpSessionTools
{
    /// <summary>
    /// Describes the servers.
    /// </summary>
    /// <param name="tools">The session's servers.</param>
    /// <param name="supportsHttp">Whether the agent connects to remote servers over HTTP.</param>
    /// <returns>The <c language="csharp">mcpServers</c> member.</returns>
    public static JsonArray For(IEnumerable<BuildAgentTool> tools, bool supportsHttp)
    {
        var servers = new JsonArray();
        foreach (var tool in tools)
        {
            if (tool.Command is not null)
            {
                servers.Add(new JsonObject
                {
                    ["name"] = tool.Name,
                    ["command"] = tool.Command,
                    ["args"] = new JsonArray([.. tool.Arguments.Select(argument => (JsonNode)argument)]),
                    ["env"] = new JsonArray()
                });
            }
            else if (supportsHttp && tool.Url is not null)
            {
                var headers = new JsonArray();
                if (tool.BearerToken is not null) headers.Add(new JsonObject { ["name"] = "Authorization", ["value"] = $"Bearer {tool.BearerToken}" });
                foreach (var (name, value) in tool.Headers) headers.Add(new JsonObject { ["name"] = name, ["value"] = value });
                servers.Add(new JsonObject { ["type"] = "http", ["name"] = tool.Name, ["url"] = tool.Url, ["headers"] = headers });
            }
        }

        return servers;
    }
}
