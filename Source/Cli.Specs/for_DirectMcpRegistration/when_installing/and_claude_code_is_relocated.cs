// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class and_claude_code_is_relocated : given.a_home_and_a_project
{
    void Establish()
    {
        _environment["CLAUDE_CONFIG_DIR"] = HomeFile("claude-config");
        Write(HomeFile(".claude.json"), "{}");
    }

    void Because() => _plan = Install(DirectMcpScope.User, ["claude"]);

    [Fact] void should_report_claude_as_unsupported() => _plan.Unsupported.Single().ShouldContain("CLAUDE_CONFIG_DIR");
    [Fact] void should_report_that_nothing_could_be_registered() => _plan.NothingRegistrable.ShouldBeTrue();
    [Fact] void should_leave_the_home_configuration_alone() => File.ReadAllText(HomeFile(".claude.json")).ShouldEqual("{}");
    [Fact] void should_not_claim_ownership() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
