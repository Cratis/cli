// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_setup_is_repeated : given.a_setup
{
    async Task Establish()
    {
        await Run();
        _valid = true;
    }
    async Task Because() => _result = await Run();
    [Fact] void should_leave_the_registration_unchanged() => _result.Clients.Single().State.ShouldEqual("already configured");
    [Fact] void should_not_repeat_login() => _logins.ShouldEqual(1);
}
