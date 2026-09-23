// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_uninstalling;

public class with_user_owned_content : given.a_screenplay_corpus
{
    void Establish()
    {
        Write(".mcp.json", """{"mcpServers":{"other":{"command":"keep-me"}},"user":42}""");
        Install();
        Write(".cratis/screenplay/model.play", "model owned by user");
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project);

    [Fact] void should_remove_only_the_owned_server() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("screenplay").ShouldBeFalse();
    [Fact] void should_preserve_the_other_server() => Read(".mcp.json")["mcpServers"]!["other"]!["command"]!.GetValue<string>().ShouldEqual("keep-me");
    [Fact] void should_preserve_the_user_property() => Read(".mcp.json")["user"]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_preserve_all_model_files() => File.ReadAllText(ProjectFile(".cratis/screenplay/model.play")).ShouldEqual("model owned by user");
    [Fact] void should_preserve_the_project_selection() => File.Exists(ProjectFile(".cratis/ai.json")).ShouldBeTrue();
}
