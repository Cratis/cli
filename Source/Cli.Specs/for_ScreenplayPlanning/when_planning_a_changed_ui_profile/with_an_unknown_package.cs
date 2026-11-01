// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

public class with_an_unknown_package : given.the_screen_composition_corpus
{
    void Establish() => ChangeApplication("^( +)Cratis\\.Components$", "$1Cratis.NotAPackage");

    Task Because() => Plan();

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_unknown_package() => ErrorsWith(SceneProfileResolution.UnknownPackageCode).Single().Message.ShouldContain("'Cratis.NotAPackage'");
}
