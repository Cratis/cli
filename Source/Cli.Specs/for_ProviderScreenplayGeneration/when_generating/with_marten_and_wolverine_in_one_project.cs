// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_marten_and_wolverine_in_one_project : given.an_application_scope
{
    GeneratedScreenplay _result = null!;

    async Task Because() => _result = await Generate(
        LoadedFrom(Project("Application", CritterStackPackages, false, MartenSource, WolverineSource)));

    [Fact] void should_report_critter_stack() => _result.Provenance!.Provider.ShouldEqual(ScreenplayProviders.CritterStack);
    [Fact] void should_execute_the_complete_critter_stack_facade() => _result.Source.ShouldContain("reducer AccountSnapshot => Account");
    [Fact] void should_keep_all_critter_stack_package_evidence() => _result.Provenance!.Projects.Single().Packages.ShouldContainOnly(CritterStackPackages);
}
