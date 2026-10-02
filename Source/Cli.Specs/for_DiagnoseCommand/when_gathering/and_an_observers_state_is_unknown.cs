// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_an_observers_state_is_unknown : given.healthy_services
{
    void Establish() => _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(Task.FromResult<IEnumerable<ObserverInformation>>(
        [new ObserverInformation { Id = "unknown-observer", RunningState = ObserverRunningState.Unknown }]));

    async Task Because()
    {
        _data = await DiagnoseCommand.Gather(_services, _settings);
        CaptureReports();
    }

    [Fact] void should_count_the_unknown_observer() => _data.TotalObservers.ShouldEqual(1);
    [Fact] void should_count_the_unknown_observer_in_its_scope() => _data.Scopes.Single().TotalObservers.ShouldEqual(1);
    [Fact] void should_not_count_the_unknown_observer_as_active() => _data.ActiveObservers.ShouldEqual(0);
    [Fact] void should_include_the_unknown_observer_in_the_json_total() => JsonDocument.Parse(_outputs[OutputFormats.Json]).RootElement.GetProperty("observers").GetProperty("total").GetInt32().ShouldEqual(1);
    [Fact] void should_not_claim_no_observers_exist_in_text() => _outputs[OutputFormats.Table].ShouldNotContain("none; 0 quarantined");
    [Fact] void should_not_claim_no_observers_exist_in_watch() => _outputs["watch"].ShouldNotContain("none; 0 quarantined");
    [Fact] void should_keep_the_complete_sweep_healthy() => _data.IsHealthy.ShouldBeTrue();
}
