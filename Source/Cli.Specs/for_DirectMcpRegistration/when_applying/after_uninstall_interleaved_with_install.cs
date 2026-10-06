// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_uninstall_interleaved_with_install : given.a_home_and_a_project
{
    Exception? _error;

    void Establish()
    {
        Install(DirectMcpScope.User, ["claude"]);
        _plan = DirectMcpRegistration.Install(DirectMcpScope.User, Locations, ["cursor"], DirectMcpClients.Arguments(Origin, null));
        Uninstall(DirectMcpScope.User);
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing));

    [Fact] void should_reject_the_stale_install() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_recreate_the_manifest() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
    [Fact] void should_not_install_the_second_client() => File.Exists(HomeFile(".cursor/mcp.json")).ShouldBeFalse();
}
