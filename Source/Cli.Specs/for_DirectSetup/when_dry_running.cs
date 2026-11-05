// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectSetup;

public class when_dry_running : given.a_setup
{
    void Establish() => _settings.DryRun = true;
    async Task Because() => _result = await Run();
    [Fact] void should_not_check_tokens() => _checks.ShouldEqual(0);
    [Fact] void should_not_login() => _logins.ShouldEqual(0);
    [Fact] void should_not_write_files() => Directory.GetFiles(_home, "*", SearchOption.AllDirectories).ShouldBeEmpty();
    [Fact] void should_report_the_plan() => _result.Clients.Single().State.ShouldEqual("would install");
    [Fact] void should_explain_login() => _result.Login.ShouldContain("Would check");
}
