// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_is_suspended : given.required_observer_services
{
    void Establish() => SetObserver(ObserverRunningState.Suspended);

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_satisfy_the_requirement() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.Unsatisfied);
    [Fact] void should_explain_the_failure() => Requirement.Reason.ShouldEqual("required observer suspended");
    [Fact] void should_not_add_a_finding() => _data.Findings.ShouldBeEmpty();
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
