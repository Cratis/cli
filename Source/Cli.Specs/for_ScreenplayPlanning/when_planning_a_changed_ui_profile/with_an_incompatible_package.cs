// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

/// <summary>
/// A profile selecting a theme that does not declare itself compatible with one of the profile's packages.
/// </summary>
public class with_an_incompatible_package : given.the_screen_composition_corpus
{
    void Establish()
    {
        ChangeApplication("^ui profile Desktop$", "theme Midnight\n  compatible with core\n\nui profile Desktop");
        ChangeApplication("^( +)target size expanded$", "$1target size expanded\n$1theme Midnight");
    }

    Task Because() => Plan();

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_incompatible_package() => ErrorsWith(SceneProfileResolution.IncompatiblePackageCode).Single().Message.ShouldContain("'Cratis.Components'");
}
