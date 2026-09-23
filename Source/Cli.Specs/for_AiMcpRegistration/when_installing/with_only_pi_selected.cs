// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_only_pi_selected : given.a_screenplay_corpus
{
    void Establish() => _configuration = _configuration with { Harnesses = ["pi"] };
    void Because() => _result = Install();

    [Fact] void should_create_the_model_root_for_native_extension_startup() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeTrue();
    [Fact] void should_install_the_native_extension_through_normal_discovery() => File.Exists(ProjectFile(".pi/extensions/cratis-mcp.ts")).ShouldBeTrue();
    [Fact] void should_report_the_extension_in_status() => AiCorpusSynchronizer.Status(_project).McpExtensions!.ShouldContain("pi/screenplay");
    [Fact] void should_not_create_an_invented_pi_mcp_config() => File.Exists(ProjectFile(".pi/mcp.json")).ShouldBeFalse();
    [Fact] void should_not_report_pi_as_unsupported() => _result.UnsupportedMcpServers!.ShouldBeEmpty();
}
