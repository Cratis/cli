// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_McpHttpBridge.given;

public class a_bridged_server : Specification
{
    /// <summary>A stand-in MCP server: it logs a line of its own, then answers every request with its id and method, and ignores notifications.</summary>
    static readonly string Server = string.Join('\n', [
        "echo '{\"jsonrpc\":\"2.0\",\"method\":\"notifications/message\",\"params\":{}}'",
        "while IFS= read -r line; do",
        "  case \"$line\" in",
        "    *'\"id\":'*) id=$(printf '%s' \"$line\" | sed 's/.*\"id\":\\([^,}]*\\).*/\\1/'); method=$(printf '%s' \"$line\" | sed 's/.*\"method\":\"\\([^\"]*\\)\".*/\\1/'); printf '{\"jsonrpc\":\"2.0\",\"id\":%s,\"result\":{\"method\":\"%s\"}}\\n' \"$id\" \"$method\";;",
        "  esac",
        "done"
    ]);

    protected McpHttpBridge _bridge;
    protected BuildAgentTool _described;
    protected HttpClient _client;

    void Establish()
    {
        _bridge = McpHttpBridge.Start("/bin/sh", ["-c", Server], Path.GetTempPath());
        _described = _bridge.Describe(new BuildAgentTool("screenplay", "/bin/sh", ["-c", Server], null, null, ["read"]));
        _client = new HttpClient();
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _bridge.DisposeAsync();
    }

    protected Task<HttpResponseMessage> Post(string body, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _described.Url) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        if (token is not null) request.Headers.Add("Authorization", $"Bearer {token}");
        return _client.SendAsync(request);
    }
}
