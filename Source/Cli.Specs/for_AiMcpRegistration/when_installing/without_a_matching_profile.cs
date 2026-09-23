// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class without_a_matching_profile : given.a_screenplay_corpus
{
    void Establish() => _configuration = _configuration with { Profiles = ["cratis/other"] };
    void Because() => _result = Install();

    [Fact] void should_not_register_screenplay() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
    [Fact] void should_still_carry_the_descriptor() => File.Exists(ProjectFile(".cratis/ai/mcp-servers.json")).ShouldBeTrue();
    [Fact] void should_not_create_a_model_directory() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeFalse();
}
