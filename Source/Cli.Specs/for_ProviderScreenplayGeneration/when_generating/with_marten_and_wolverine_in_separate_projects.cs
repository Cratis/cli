// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_marten_and_wolverine_in_separate_projects : given.an_application_scope
{
    GeneratedScreenplay _result = null!;

    async Task Because() => _result = await Generate(
        LoadedFrom(
            Project(
                "Persistence",
                [new ResolvedScreenplayPackage("Marten", "9.23.0"), new ResolvedScreenplayPackage("WolverineFx.Marten", "6.29.1")],
                false,
                MartenSource),
            Project("Worker", [new ResolvedScreenplayPackage("WolverineFx", "6.29.1")], false, WolverineSource)));

    [Fact] void should_report_critter_stack_for_the_application_scope() => _result.Provenance!.Provider.ShouldEqual(ScreenplayProviders.CritterStack);
    [Fact] void should_execute_the_complete_facade_across_both_projects() => _result.Source.ShouldContain("reducer AccountSnapshot => Account");
    [Fact] void should_report_both_projects() => _result.Projects.ShouldContainOnly(["Persistence", "Worker"]);
    [Fact] void should_keep_each_projects_package_evidence_separate() => _result.Provenance!.Projects.Select(project => project.Packages.Count).ShouldContainOnly([2, 1]);
}
