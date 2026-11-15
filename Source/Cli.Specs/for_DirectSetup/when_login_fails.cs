// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_login_fails : given.a_setup
{
    void Establish() => _loginExit = ExitCodes.ValidationError;
    async Task Because() => _result = await Run();
    [Fact] void should_fail() => _result.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_register_clients() => _result.Clients.ShouldBeEmpty();
    [Fact] void should_not_write_files() => Directory.GetFiles(_home, "*", SearchOption.AllDirectories).ShouldBeEmpty();
}
