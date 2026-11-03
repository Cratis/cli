// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_id_is_repeated : given.required_observer_services
{
    void Establish()
    {
        _settings.RequireObserver = ["required", "required", "REQUIRED"];
        SetObserver(ObserverRunningState.Active);
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_deduplicate_ordinally() => _data.ObserverRequirements.Count.ShouldEqual(2);
    [Fact] void should_match_the_exact_id() => _data.ObserverRequirements.Single(result => result.ObserverId == "required").Status.ShouldEqual(DiagnoseObserverRequirementStatus.Satisfied);
    [Fact] void should_not_match_different_casing() => _data.ObserverRequirements.Single(result => result.ObserverId == "REQUIRED").Status.ShouldEqual(DiagnoseObserverRequirementStatus.Absent);
}
