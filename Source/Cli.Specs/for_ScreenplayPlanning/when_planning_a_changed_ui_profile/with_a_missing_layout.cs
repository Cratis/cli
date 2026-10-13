// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

public class with_a_missing_layout : given.the_screen_composition_corpus
{
    void Establish() => ChangeApplication("^( +)layout AppShell$", "$1layout MissingShell");

    Task Because() => Plan();

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_missing_layout() => ErrorsWith(SceneProfileResolution.MissingLayoutCode).Single().Message.ShouldContain("'MissingShell'");
}
