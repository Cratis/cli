// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_reading_status;

public class with_a_disallowed_managed_launch : given.a_screenplay_corpus
{
    AiStatus _status;

    void Establish()
    {
        Install();
        var host = Read(".mcp.json");
        host["mcpServers"]!["screenplay"]!["args"]!.AsArray().Add("--unexpected");
        Write(".mcp.json", host.ToJsonString());
        var manifest = Read(".cratis/ai.manifest.json");
        manifest["McpServers"]!.AsArray().Single(entry => entry!["Harness"]!.GetValue<string>() == "claude")!["Installed"]!["args"]!.AsArray().Add("--unexpected");
        Write(".cratis/ai.manifest.json", manifest.ToJsonString());
    }

    void Because() => _status = AiCorpusSynchronizer.Status(_project);

    [Fact] void should_flag_the_disallowed_launch_even_when_the_manifest_matches() => _status.ModifiedFiles.Single().ShouldContain("managed MCP launch differs");
}
