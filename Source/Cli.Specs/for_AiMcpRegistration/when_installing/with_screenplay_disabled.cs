// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_screenplay_disabled : given.a_screenplay_corpus
{
    void Establish() => _configuration = _configuration with { McpServers = new Dictionary<string, AiMcpConfiguration> { ["screenplay"] = new(false) } };
    void Because() => _result = Install();

    [Fact] void should_not_register_screenplay() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
    [Fact] void should_roundtrip_disabled_state() => AiCorpusSynchronizer.Status(_project).Configuration.McpServers!["screenplay"].Enabled.ShouldBeFalse();
    [Fact] void should_not_report_disabled_server_as_unsupported() => _result.UnsupportedMcpServers!.ShouldBeEmpty();
}
