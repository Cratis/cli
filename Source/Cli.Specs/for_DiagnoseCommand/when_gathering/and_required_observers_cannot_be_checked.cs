// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_required_observers_cannot_be_checked : given.required_observer_services
{
    void Establish() => _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns(Task.FromException<IEnumerable<ObserverInformation>>(new InvalidOperationException("unavailable")));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_not_claim_absence() => Requirement.Status.ShouldEqual(DiagnoseObserverRequirementStatus.CouldNotCheck);
    [Fact] void should_explain_the_unavailable_check() => Requirement.Reason.ShouldEqual("see Could not check");
    [Fact] void should_have_no_observed_state() => Requirement.ObservedState.ShouldBeNull();
    [Fact] void should_keep_the_existing_check_failure() => _data.ChecksCouldNotRun.Single().Check.ShouldEqual("Observers");
    [Fact] void should_be_incomplete() => _data.ChecksComplete.ShouldBeFalse();
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
