// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_watching;

[Collection(CliSpecsCollection.Name)]
public class and_no_sweep_has_run : given.watch_services
{
    void Establish() => _cancellation.Cancel();

    async Task Because() => _exitCode = await DiagnoseCommand.RunWatch(_services, _settings, _cancellation.Token);

    [Fact] void should_not_start_a_sweep() => _services.Server.DidNotReceive().GetVersionInfo();
    [Fact] void should_return_server_error() => _exitCode.ShouldEqual(ExitCodes.ServerError);
}
