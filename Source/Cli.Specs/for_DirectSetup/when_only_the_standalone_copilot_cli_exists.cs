// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_only_the_standalone_copilot_cli_exists : given.a_setup
{
    void Establish()
    {
        _settings.Clients = [];
        Directory.CreateDirectory(HomeFile(".copilot"));
    }

    async Task Because() => _result = await Run();

    [Fact] void should_not_register_the_vs_code_integration() => _result.Clients.ShouldBeEmpty();
    [Fact] void should_request_explicit_selection() => _result.Login.ShouldContain("--client");
    [Fact] void should_not_log_in() => _logins.ShouldEqual(0);
}
