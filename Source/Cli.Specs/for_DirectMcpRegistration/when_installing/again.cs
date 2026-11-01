// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class again : given.a_home_and_a_project
{
    string _configuration;
    string _manifest;

    void Establish()
    {
        Install(DirectMcpScope.User, _every);
        _configuration = File.ReadAllText(HomeFile(".codex/config.toml"));
        _manifest = File.ReadAllText(HomeFile(DirectMcpManifest.RelativePath));
    }

    void Because() => _plan = Install(DirectMcpScope.User, _every);

    [Fact] void should_change_nothing() => _plan.Changes.ShouldBeEmpty();
    [Fact] void should_have_no_conflicts() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_the_configuration_as_it_was() => File.ReadAllText(HomeFile(".codex/config.toml")).ShouldEqual(_configuration);
    [Fact] void should_leave_the_ownership_record_as_it_was() => File.ReadAllText(HomeFile(DirectMcpManifest.RelativePath)).ShouldEqual(_manifest);
}
