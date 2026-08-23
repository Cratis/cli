// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_marten_only : given.an_application_scope
{
    GeneratedScreenplay _result = null!;

    async Task Because() => _result = await Generate(
        LoadedFrom(Project("Persistence", MartenPackages, false, MartenSource)));

    [Fact] void should_report_marten() => _result.Provenance!.Provider.ShouldEqual(ScreenplayProviders.Marten);
    [Fact] void should_report_the_critter_stack_facade_version_as_the_marten_provider_version() => _result.Provenance!.ProviderVersion.ShouldEqual("0.19.0");
    [Fact] void should_execute_the_complete_critter_stack_facade() => _result.Source.ShouldContain("reducer AccountSnapshot => Account");
    [Fact] void should_report_only_marten_package_evidence() => _result.Provenance!.Projects.Single().Packages.ShouldContainOnly(MartenPackages);
}
