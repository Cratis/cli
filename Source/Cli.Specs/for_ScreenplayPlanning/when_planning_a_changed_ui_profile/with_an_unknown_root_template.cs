// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

/// <summary>
/// A root template nothing declares is the compiler's <c language="shell">PLAY0790</c>, reported before anything is planned (Cratis/Stage#466).
/// </summary>
public class with_an_unknown_root_template : given.the_screen_composition_corpus
{
    void Establish() => ChangeApplication("^( +)root template AppShell$", "$1root template MissingShell");

    Task Because() => Plan();

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_unknown_root_template() => ErrorsWith("PLAY0790").Single().Message.ShouldContain("MissingShell");
}
