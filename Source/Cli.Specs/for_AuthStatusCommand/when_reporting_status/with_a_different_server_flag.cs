// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class with_a_different_server_flag : given.an_auth_status
{
    void Establish() => _settings.Server = "chronicle://other:35000";
    async Task Because() => await Execute();

    [Fact] void should_show_the_bound_server() => _status.RootElement.GetProperty("tokenServer").GetString().ShouldEqual("production:35000");
    [Fact] void should_show_the_selected_server() => _status.RootElement.GetProperty("server").GetString().ShouldEqual("chronicle://other:35000");
    [Fact] void should_report_the_mismatch() => _status.RootElement.GetProperty("loginMatchesServer").GetBoolean().ShouldBeFalse();
}
