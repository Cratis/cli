// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_observers_cannot_be_checked : given.healthy_services
{
    void Establish() => _services.Observers.GetObservers(Arg.Any<AllObserversRequest>())
        .Returns(Task.FromException<IEnumerable<ObserverInformation>>(new Exception("Observer query timed out")));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_be_unhealthy() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_report_the_reason() => _data.ChecksCouldNotRun.ShouldContainOnly(new DiagnoseCheckFailure("Observers", "store", "tenant-one", "Observer query timed out"));
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
