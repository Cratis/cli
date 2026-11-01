// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_a_scope_is_selected : given.a_release_baseline
{
    void Establish() => _request = Changed(Source.Replace("value String", "value Int", StringComparison.Ordinal)) with { Scope = ["Records.Recording.Observe"] };
    void Because() => Plan();

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unselected_sibling_violation() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-001" && diagnostic.Message.Contains("Recorded", StringComparison.Ordinal));
    [Fact] void should_not_render_the_unselected_event() => _result.Artifacts!.Artifacts.SelectMany(artifact => artifact.Sources).Select(source => _result.SemanticAddresses.Resolve(source.ToString()).Address).ShouldNotContain("Records/Recording/Record/Recorded");
    [Fact] void should_attribute_artifacts_to_the_selected_event() => _result.Artifacts!.Artifacts.SelectMany(artifact => artifact.Sources).Select(source => _result.SemanticAddresses.Resolve(source.ToString()).Address).ShouldContain("Records/Recording/Observe/Removed");
}
