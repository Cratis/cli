// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class with_an_environment_server : given.an_auth_status
{
    void Establish() => Environment.SetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar, "chronicle://environment:35000");
    async Task Because() => await Execute();

    [Fact] void should_show_the_context_server() => _status.RootElement.GetProperty("server").GetString().ShouldEqual("chronicle://production:35000");
    [Fact] void should_report_the_mismatch() => _status.RootElement.GetProperty("loginMatchesServer").GetBoolean().ShouldBeFalse();
}
