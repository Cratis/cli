// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_unrequired_observers_are_not_running : given.healthy_services
{
    void Establish() => _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(Task.FromResult<IEnumerable<ObserverInformation>>(
    [
        new ObserverInformation { Id = "disconnected", RunningState = ObserverRunningState.Disconnected },
        new ObserverInformation { Id = "suspended", RunningState = ObserverRunningState.Suspended }
    ]));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_have_no_requirements() => _data.ObserverRequirements.ShouldBeEmpty();
    [Fact] void should_preserve_health() => _data.IsHealthy.ShouldBeTrue();
    [Fact] void should_exit_successfully() => _data.ExitCode.ShouldEqual(ExitCodes.Success);
}
