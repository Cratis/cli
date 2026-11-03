// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseData.when_checking_health;

public class and_a_required_observer_is_not_satisfied : Specification
{
    [Theory]
    [InlineData(DiagnoseObserverRequirementStatus.Absent)]
    [InlineData(DiagnoseObserverRequirementStatus.Unsatisfied)]
    [InlineData(DiagnoseObserverRequirementStatus.CouldNotCheck)]
    void should_affect_health_and_exit_code(DiagnoseObserverRequirementStatus status)
    {
        var data = new DiagnoseData("localhost", "store", "tenant-one", true, "19.6.1", null, [], 0, 0, 0, 0, 0, 0, null, DateTimeOffset.UtcNow)
        {
            ObserverRequirements = [new("required", "store", "tenant-one", status, null, "reason")]
        };
        data.IsHealthy.ShouldBeFalse();
        data.ExitCode.ShouldEqual(ExitCodes.ServerError);
    }
}
