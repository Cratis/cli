// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_is_replaying : given.required_observer_services
{
    void Establish() => SetObserver(ObserverRunningState.Replaying);

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_satisfy_the_requirement() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.Satisfied);
    [Fact] void should_report_replay() => Requirement.Reason.ShouldEqual("running; replaying");
    [Fact] void should_exit_successfully() => _data.ExitCode.ShouldEqual(ExitCodes.Success);
}
