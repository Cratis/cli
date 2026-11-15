// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_a_required_observer_is_absent : given.required_observer_services
{
    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_report_absence() => Requirement.ShouldEqual(new DiagnoseObserverRequirement("required", "store", "tenant-one", DiagnoseObserverRequirementStatus.Absent, null, "not registered in store/namespace"));
    [Fact] void should_complete_the_checks() => _data.ChecksComplete.ShouldBeTrue();
    [Fact] void should_not_duplicate_the_requirement_in_findings() => _data.Findings.ShouldBeEmpty();
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
