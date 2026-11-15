// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observers_state_is_unrecognized : given.required_observer_services
{
    void Establish() => SetObserver((ObserverRunningState)999);

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_satisfy_the_requirement() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.Unsatisfied);
    [Fact] void should_explain_the_failure() => Requirement.Reason.ShouldEqual("unrecognized state 999");
    [Fact] void should_retain_the_observed_value() => Requirement.ObservedState.ShouldEqual("999");
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
