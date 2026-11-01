// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class with_an_invalid_server : given.an_auth_status
{
    void Establish() => _settings.Server = "chronicle://other:not-a-port";
    async Task Because() => await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_mismatch() => _status.RootElement.GetProperty("loginMatchesServer").GetBoolean().ShouldBeFalse();
}
