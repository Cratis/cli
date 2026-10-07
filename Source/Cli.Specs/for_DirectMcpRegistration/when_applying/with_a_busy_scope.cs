// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class with_a_busy_scope : given.a_home_and_a_project
{
    Exception? _error;
    IDisposable _held;

    void Establish()
    {
        _plan = DirectMcpRegistration.Install(DirectMcpScope.User, Locations, ["claude"], DirectMcpClients.Arguments(Origin, null));
        _held = DirectMcpManifest.AcquireLock(_home, _home);
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing));

    [Fact] void should_refuse_a_concurrent_apply() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_write_the_client_file() => File.Exists(HomeFile(".claude.json")).ShouldBeFalse();
    [Fact] void should_not_write_the_manifest() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();

    void Destroy() => _held.Dispose();
}
