// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_an_observer_is_quarantined : given.healthy_services
{
    void Establish() => _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(Task.FromResult<IEnumerable<ObserverInformation>>(
    [
        new ObserverInformation { Id = "quarantined", RunningState = ObserverRunningState.Quarantined },
        new ObserverInformation { Id = "active", RunningState = ObserverRunningState.Active }
    ]));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_be_unhealthy() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_count_the_quarantined_observer() => _data.QuarantinedObservers.ShouldEqual(1);
    [Fact] void should_include_quarantined_in_the_total() => _data.TotalObservers.ShouldEqual(2);
    [Fact] void should_name_the_quarantined_observer_and_scope() => _data.Findings.ShouldContainOnly(new DiagnoseFinding("Quarantined observer", "store", "tenant-one", "quarantined"));
    [Fact] void should_have_completed_the_checks() => _data.ChecksComplete.ShouldBeTrue();
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
