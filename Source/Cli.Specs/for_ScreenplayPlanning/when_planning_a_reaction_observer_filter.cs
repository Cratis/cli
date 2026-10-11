// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_reaction_observer_filter : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

#pragma warning disable MA0136 // The Screenplay compiler accepts either line ending in model source.
    void Establish() => File.WriteAllText(_file, """
        eventsource Account
          identifier String
          stream Entries
        module Banking
          feature Booking
            slice Automation FollowUp
              event Booked
              event Followed
              reaction Follow
                from Account.Entries
                when Booked
                  produces Followed
        """);
#pragma warning restore MA0136

    async Task Because() => _result = await Plan();

    [Fact] void should_refuse_the_reaction_filter() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-035");
    [Fact] void should_not_refuse_the_version() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_not_report_source_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("PLAY", StringComparison.Ordinal) && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_plan_artifacts() => _result.Artifacts!.Artifacts.ShouldBeEmpty();
}
