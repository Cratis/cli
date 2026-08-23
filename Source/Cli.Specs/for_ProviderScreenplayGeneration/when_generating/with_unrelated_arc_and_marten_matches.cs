// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_unrelated_arc_and_marten_matches : given.an_application_scope
{
    GeneratedScreenplay _result = null!;

    async Task Because() => _result = await Generate(
        LoadedFrom(
            Project("Arc", [], false, ArcSource),
            Project("Persistence", MartenPackages, false, MartenSource)));

    [Fact] void should_not_choose_a_provider() => _result.Provenance.ShouldBeNull();
    [Fact] void should_not_execute_either_complete_facade() => _result.Source.ShouldBeEmpty();
    [Fact] void should_report_ambiguous_unrelated_matches() => _result.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.AmbiguousProviders);
    [Fact] void should_name_arc_and_marten_in_stable_order() => _result.Diagnostics.Single().Message.ShouldContain("arc, marten");
}
