// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_login_is_valid : given.a_setup
{
    void Establish() => _valid = true;
    async Task Because() => _result = await Run();
    [Fact] void should_check_login_once() => _checks.ShouldEqual(1);
    [Fact] void should_not_login() => _logins.ShouldEqual(0);
    [Fact] void should_install() => _result.Clients.Single().State.ShouldEqual("installed");
}
