// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_existing_host_configuration : given.a_screenplay_corpus
{
    void Establish()
    {
        Write(".mcp.json", """{"custom":{"nested":[1,2]},"mcpServers":{"other":{"command":"user-tool","env":{"TOKEN":"keep"}}}}""");
        Write(".vscode/mcp.json", """{"inputs":[{"id":"secret","type":"promptString"}],"servers":{"other":{"command":"user-tool"}}}""");
        Write("opencode.json", """{"model":"user-model","mcp":{"other":{"type":"remote","url":"https://example.test"}}}""");
    }

    void Because() => _result = Install();

    [Fact] void should_preserve_other_servers() => Read(".mcp.json")["mcpServers"]!["other"]!["command"]!.GetValue<string>().ShouldEqual("user-tool");
    [Fact] void should_preserve_nested_properties() => Read(".mcp.json")["custom"]!["nested"]!.AsArray().Count.ShouldEqual(2);
    [Fact] void should_preserve_inputs() => Read(".vscode/mcp.json")["inputs"]![0]!["id"]!.GetValue<string>().ShouldEqual("secret");
    [Fact] void should_preserve_opencode_settings() => Read("opencode.json")["model"]!.GetValue<string>().ShouldEqual("user-model");
}
