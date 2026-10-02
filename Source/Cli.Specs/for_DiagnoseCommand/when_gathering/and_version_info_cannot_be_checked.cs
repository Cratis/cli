// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Host;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_version_info_cannot_be_checked : given.healthy_services
{
    void Establish() => _services.Server.GetVersionInfo().Returns(Task.FromException<ServerVersionInfo>(new Exception("Version query failed")));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_report_the_server_as_unreachable() => _data.ServerReachable.ShouldBeFalse();
    [Fact] void should_report_the_connection_check_failure() => _data.ChecksCouldNotRun.ShouldContainOnly(new DiagnoseCheckFailure("Connection", null, null, "Version query failed"));
    [Fact] void should_be_unhealthy() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
