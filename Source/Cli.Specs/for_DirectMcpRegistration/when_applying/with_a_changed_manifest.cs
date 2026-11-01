// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class with_a_changed_manifest : given.a_home_and_a_project
{
    const string Changed = "{\"Servers\":[]}\n";
    Exception? _error;

    void Establish()
    {
        _plan = DirectMcpRegistration.Install(DirectMcpScope.User, Locations, ["claude"], DirectMcpClients.Arguments(Origin, null));
        Write(HomeFile(DirectMcpManifest.RelativePath), Changed);
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing));

    [Fact] void should_reject_even_semantically_equivalent_changes() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_preserve_the_external_manifest() => File.ReadAllText(HomeFile(DirectMcpManifest.RelativePath)).ShouldEqual(Changed);
    [Fact] void should_not_write_the_client_file() => File.Exists(HomeFile(".claude.json")).ShouldBeFalse();
}
