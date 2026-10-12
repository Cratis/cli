// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

public class with_a_custom_package_that_is_not_approved : given.an_approved_package
{
    Task Because() => Plan();

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_unknown_package() => ErrorsWith(SceneProfileResolution.UnknownPackageCode).Single().Message.ShouldContain("'Acme.Charts'");
}
