// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class pi_outside_home : given.a_home_and_a_project
{
    void Establish() => _environment["PI_CODING_AGENT_DIR"] = Path.Combine(_home, "..", "external-agent");
    void Because() => _plan = Install(DirectMcpScope.User, ["pi"]);
    [Fact] void should_name_the_variable() => _plan.Unsupported.Single().ShouldContain("PI_CODING_AGENT_DIR");
    [Fact] void should_suggest_unsetting_it() => _plan.Unsupported.Single().ShouldContain("unset");
    [Fact] void should_suggest_project_scope() => _plan.Unsupported.Single().ShouldContain("--scope project");
    [Fact] void should_write_nothing() => Directory.GetFiles(_home, "*", SearchOption.AllDirectories).ShouldBeEmpty();
}
