// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class without_the_native_pi_extension : given.a_screenplay_corpus
{
    void Establish()
    {
        _configuration = _configuration with { Harnesses = ["pi"] };
        File.Delete(Path.Combine(_corpus, ".cratis/ai/harnesses/pi/extensions/cratis-mcp.ts"));
    }

    void Because() => _result = Install();

    [Fact] void should_report_the_missing_extension_explicitly() => _result.UnsupportedMcpServers!.Single().ShouldContain("native cratis-mcp Pi extension");
    [Fact] void should_not_claim_a_native_extension_is_configured() => AiCorpusSynchronizer.Status(_project).McpExtensions!.ShouldBeEmpty();
    [Fact] void should_still_install_the_selected_guidance() => File.Exists(ProjectFile(".cratis/ai/rules/general.md")).ShouldBeTrue();
}
