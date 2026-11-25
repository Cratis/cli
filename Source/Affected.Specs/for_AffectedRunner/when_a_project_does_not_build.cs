// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Affected.Fixtures;

namespace Cratis.Affected.for_AffectedRunner;

public class when_a_project_does_not_build : Specification
{
    FakeTestRunner _runner;
    int _exitCode;

    async Task Because()
    {
        _runner = new FakeTestRunner([], unbuildable: new HashSet<string> { "Source/B.Specs/B.Specs.csproj" });
        var report = new SelectionReport(
            PlanMode.PullRequest,
            SelectionLevel.Test,
            "base",
            "head",
            false,
            [],
            [],
            [
                new ProjectPlan(ProjectSelection.RunAll("Source/A.Specs/A.Specs.csproj", "test"), []),
                new ProjectPlan(ProjectSelection.RunAll("Source/B.Specs/B.Specs.csproj", "test"), []),
                new ProjectPlan(ProjectSelection.RunAll("Source/C.Specs/C.Specs.csproj", "test"), [])
            ]);
        _exitCode = await new AffectedRunner(_runner).RunAsync(report, Path.Combine(Path.GetTempPath(), "affected-runner-spec"));
    }

    [Fact] void should_fail_the_run() => _exitCode.ShouldNotEqual(0);
    [Fact] void should_still_run_the_projects_before_and_after_it() => _runner.RunProjects.ShouldContainOnly("Source/A.Specs/A.Specs.csproj", "Source/C.Specs/C.Specs.csproj");
}
