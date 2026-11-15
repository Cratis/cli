// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_AcpSessionTools;

public class when_a_server_carries_the_modeling_session : Specification
{
    const string SessionId = "1d2c3b4a-5e6f-4a7b-8c9d-0e1f2a3b4c5d";
    IReadOnlyList<BuildAgentTool> _tools;
    JsonArray _acp;
    JsonObject _claude;

    void Establish() => _tools = [.. given.a_session.With().Tools.Select(tool => tool.Url is null
        ? tool
        : tool with { Headers = new Dictionary<string, string> { [ModelingSession.SessionHeader] = SessionId } })];

    void Because()
    {
        _acp = AcpSessionTools.For(_tools, supportsHttp: true);
        _claude = ClaudeCodeArguments.McpConfiguration(_tools);
    }

    JsonArray AcpHeaders => (JsonArray)_acp.OfType<JsonObject>().Single(server => server["name"]!.GetValue<string>() == BuildSessionTools.Studio)["headers"]!;

    [Fact] void should_give_an_acp_agent_the_session_header() =>
        AcpHeaders.OfType<JsonObject>().Any(header => header["name"]!.GetValue<string>() == ModelingSession.SessionHeader && header["value"]!.GetValue<string>() == SessionId).ShouldBeTrue();

    [Fact] void should_keep_the_bearer_token_for_an_acp_agent() =>
        AcpHeaders.OfType<JsonObject>().Any(header => header["name"]!.GetValue<string>() == "Authorization").ShouldBeTrue();

    [Fact] void should_give_claude_code_the_session_header() =>
        _claude["mcpServers"]![BuildSessionTools.Studio]!["headers"]![ModelingSession.SessionHeader]!.GetValue<string>().ShouldEqual(SessionId);

    [Fact] void should_not_write_the_studio_token_into_claude_codes_configuration() =>
        _claude.ToJsonString().ShouldNotContain(given.a_session.StudioToken);
}
