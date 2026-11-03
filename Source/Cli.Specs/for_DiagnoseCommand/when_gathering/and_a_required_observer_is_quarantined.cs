// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_is_quarantined : given.required_observer_services
{
    void Establish() => SetObserver(ObserverRunningState.Quarantined);

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_satisfy_the_requirement() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.Unsatisfied);
    [Fact] void should_explain_the_failure() => Requirement.Reason.ShouldEqual("quarantined");
    [Fact] void should_keep_the_existing_quarantine_finding() => _data.Findings.ShouldContainOnly(new DiagnoseFinding("Quarantined observer", "store", "tenant-one", "required"));
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
