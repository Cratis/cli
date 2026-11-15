// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_login_is_missing : given.a_setup
{
    void Establish() => _settings.Tenant = "team";
    async Task Because() => _result = await Run();
    [Fact] void should_login_once() => _logins.ShouldEqual(1);
    [Fact] void should_pass_the_origin() => _loginSettings.Url.ShouldEqual(Origin);
    [Fact] void should_pass_the_tenant() => _loginSettings.Tenant.ShouldEqual("team");
    [Fact] void should_install_after_login() => File.ReadAllText(HomeFile(".codex/config.toml")).ShouldContain("cratis-direct");
    [Fact] void should_succeed() => _result.ExitCode.ShouldEqual(ExitCodes.Success);
}
