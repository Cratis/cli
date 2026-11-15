// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_is_ambiguous : given.required_observer_services
{
    void Establish() => _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(Task.FromResult<IEnumerable<ObserverInformation>>(
    [
        new ObserverInformation { Id = "required", RunningState = ObserverRunningState.Active },
        new ObserverInformation { Id = "required", RunningState = ObserverRunningState.Active }
    ]));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_satisfy_the_requirement() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.Unsatisfied);
    [Fact] void should_explain_the_ambiguity() => Requirement.Reason.ShouldEqual("ambiguous");
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
