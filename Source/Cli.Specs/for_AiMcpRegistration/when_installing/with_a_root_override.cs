// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_root_override : given.a_screenplay_corpus
{
    void Establish() => Write(".cratis/ai.json", """{"harnesses":["claude"],"profiles":["cratis/stage"],"mcpServers":{"screenplay":{"enabled":true,"root":"models/billing"}},"userProperty":"keep"}""");
    void Because() => _result = Install();

    [Fact] void should_create_the_chosen_directory() => Directory.Exists(ProjectFile("models/billing")).ShouldBeTrue();
    [Fact] void should_not_create_the_default_directory() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeFalse();
    [Fact] void should_roundtrip_the_project_owned_choice() => AiCorpusSynchronizer.Status(_project).Configuration.McpServers!["screenplay"].Root.ShouldEqual("models/billing");
    [Fact] void should_preserve_other_configuration_properties() => Read(".cratis/ai.json")["userProperty"]!.GetValue<string>().ShouldEqual("keep");
    [Fact] void should_resolve_the_override_at_server_startup() => ScreenplayMcpRoot.Resolve(null, _project, null, _project, _ => null).ShouldEqual(Path.Combine(AiProjectPaths.PhysicalRoot(_project), "models/billing"));
    [Fact] void should_not_commit_the_developer_path() => File.ReadAllText(ProjectFile(".mcp.json")).Contains(_project, StringComparison.Ordinal).ShouldBeFalse();
}
