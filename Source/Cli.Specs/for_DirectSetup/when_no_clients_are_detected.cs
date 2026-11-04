// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_no_clients_are_detected : given.a_setup
{
    void Establish() => _settings.Clients = [];
    async Task Because() => _result = await Run();
    [Fact] void should_fail() => _result.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_hint_at_explicit_selection() => _result.Login.ShouldContain("--client");
    [Fact] void should_not_login() => _logins.ShouldEqual(0);
}
