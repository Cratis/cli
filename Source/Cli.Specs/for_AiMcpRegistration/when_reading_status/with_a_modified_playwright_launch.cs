// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_reading_status;

public class with_a_modified_playwright_launch : given.a_playwright_corpus
{
    AiStatus _status;

    void Establish()
    {
        Install();
        var host = Read(".mcp.json");
        host["mcpServers"]!["playwright"]!["args"]!.AsArray().Add("--unexpected");
        Write(".mcp.json", host.ToJsonString());
        var manifest = Read(".cratis/ai.manifest.json");
        manifest["McpServers"]!.AsArray().Single(entry => entry!["Harness"]!.GetValue<string>() == "claude")!["Installed"]!["args"]!.AsArray().Add("--unexpected");
        Write(".cratis/ai.manifest.json", manifest.ToJsonString());
    }

    void Because() => _status = AiCorpusSynchronizer.Status(_project);

    [Fact] void should_flag_the_launch_that_differs_from_the_descriptor() => _status.ModifiedFiles.Single().ShouldContain("differs from the corpus descriptor");
}
