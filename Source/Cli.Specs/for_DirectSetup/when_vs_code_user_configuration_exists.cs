// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_vs_code_user_configuration_exists : given.a_setup
{
    void Establish()
    {
        _settings.Clients = [];
        Directory.CreateDirectory(HomeFile(".config/Code/User"));
    }

    async Task Because() => _result = await Run();

    [Fact] void should_detect_the_vs_code_integration() => _result.Clients.Single().Client.ShouldEqual("copilot");
    [Fact] void should_register_in_vs_code_user_configuration() => IsRegistered(HomeFile(".config/Code/User/mcp.json"), "servers").ShouldBeTrue();
}
