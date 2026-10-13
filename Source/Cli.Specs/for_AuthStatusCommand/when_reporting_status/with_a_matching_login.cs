// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class with_a_matching_login : given.an_auth_status
{
    async Task Because() => await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_show_the_bound_server() => _status.RootElement.GetProperty("tokenServer").GetString().ShouldEqual("production:35000");
    [Fact] void should_report_the_match() => _status.RootElement.GetProperty("loginMatchesServer").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_show_the_token() => _status.RootElement.GetRawText().ShouldNotContain("private-token");
}
