// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServices;

public class when_installing_and_the_service_manager_refuses : given.a_service_manager
{
    Exception _error;

    void Establish() => _answers["systemctl --user enable svc.service"] = new(1, string.Empty, "Failed to connect to bus");

    async Task Because() => _error = await Catch.Exception(() => _services.Install(_definition, CancellationToken.None));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<BuildServiceFailed>();
    [Fact] void should_say_what_the_service_manager_answered() => _error.Message.ShouldContain("Failed to connect to bus");
    [Fact] void should_not_go_on_to_start_it() => _ran.ShouldNotContain("systemctl --user restart svc.service");
}
