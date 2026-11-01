// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_another_install_interleaved_with_uninstall : given.a_home_and_a_project
{
    Exception? _error;
    byte[] _original;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        _plan = DirectMcpRegistration.Uninstall(DirectMcpScope.User, Locations, ["claude"]);
        _original = File.ReadAllBytes(HomeFile(".claude.json"));
        Install(DirectMcpScope.User, ["cursor"]);
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing));

    [Fact] void should_reject_the_stale_uninstall() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_the_registration_before_any_mutation() => File.ReadAllBytes(HomeFile(".claude.json")).ShouldEqual(_original);
    [Fact] void should_keep_both_ownership_records() => DirectMcpManifest.Read(_home).Servers.Select(server => server.Harness).ShouldContainOnly("claude", "cursor");
    [Fact] void should_not_create_a_backup() => Directory.GetFiles(_home, "*.bak", SearchOption.AllDirectories).ShouldBeEmpty();
}
