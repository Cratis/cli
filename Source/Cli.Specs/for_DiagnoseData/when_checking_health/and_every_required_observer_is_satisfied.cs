// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseData.when_checking_health;

public class and_every_required_observer_is_satisfied : Specification
{
    DiagnoseData _data = null!;

    void Establish() => _data = new DiagnoseData("localhost", "store", "tenant-one", true, "19.6.1", null, [], 0, 0, 0, 0, 0, 0, null, DateTimeOffset.UtcNow)
    {
        ObserverRequirements = [new("required", "store", "tenant-one", DiagnoseObserverRequirementStatus.Satisfied, "Active", "running")]
    };

    [Fact] void should_satisfy_the_requirements() => _data.ObserverRequirementsSatisfied.ShouldBeTrue();
    [Fact] void should_be_healthy() => _data.IsHealthy.ShouldBeTrue();
    [Fact] void should_exit_successfully() => _data.ExitCode.ShouldEqual(ExitCodes.Success);
}
