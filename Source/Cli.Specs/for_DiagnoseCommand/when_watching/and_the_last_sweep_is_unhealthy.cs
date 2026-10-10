// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_watching;

[Collection(CliSpecsCollection.Name)]
public class and_the_last_sweep_is_unhealthy : given.watch_services
{
    void Establish() => StopAfterTwoSweeps(lastHealthy: false);

    async Task Because() => _exitCode = await DiagnoseCommand.RunWatch(_services, _settings, _cancellation.Token);

    [Fact] void should_end_with_the_community_pointer() => _writer.ToString().TrimEnd().EndsWith("Questions? Ask on Discord: https://discord.gg/kt4AMpV8WV", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_complete_two_sweeps() => _sweeps.ShouldEqual(2);
    [Fact] void should_return_server_error_for_the_last_sweep() => _exitCode.ShouldEqual(ExitCodes.ServerError);
}
