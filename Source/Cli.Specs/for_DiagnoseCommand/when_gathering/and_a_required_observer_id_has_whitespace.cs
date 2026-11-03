// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_id_has_whitespace : given.required_observer_services
{
    void Establish()
    {
        _settings.RequireObserver = [" required "];
        SetObserver(ObserverRunningState.Active);
    }

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_trim_the_id() => Requirement.ObserverId.ShouldEqual(" required ");
    [Fact] void should_require_an_exact_match() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.Absent);
}
