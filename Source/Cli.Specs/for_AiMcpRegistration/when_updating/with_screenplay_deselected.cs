// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_screenplay_deselected : given.a_screenplay_corpus
{
    void Establish()
    {
        Install();
        _configuration = _configuration with { Profiles = ["cratis/other"] };
    }

    void Because() => _result = Install();

    [Fact] void should_remove_the_owned_registration() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("screenplay").ShouldBeFalse();
    [Fact] void should_leave_the_model_directory_alone() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeTrue();
    [Fact] void should_stop_reporting_unselected_unsupported_servers() => _result.UnsupportedMcpServers!.ShouldBeEmpty();
}
