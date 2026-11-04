// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_DirectSetup.given;
using Spectre.Console.Cli;

namespace Cratis.Cli.for_DirectSetup;

public class when_using_a_personal_access_token : a_setup
{
    void Establish()
    {
        _valid = true;
        _settings.Token = new FlagValue<string> { IsSet = true, Value = "dpat_example-for-specifications" };
    }

    async Task Because() => _result = await Run();

    [Fact] void should_store_the_supplied_token_even_when_a_login_exists() => _logins.ShouldEqual(1);
    [Fact] void should_not_check_an_existing_login() => _checks.ShouldEqual(0);
    [Fact] void should_forward_token_login_to_the_registration_flow() => _loginSettings.Token.IsSet.ShouldBeTrue();
    [Fact] void should_pin_no_tenant() => _loginSettings.Tenant.ShouldBeNull();
}
