// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

/// <summary>
/// The unchanged corpus profile, so the refusals above are known to come from the change and not from the check.
/// </summary>
public class with_the_authored_profile : given.the_screen_composition_corpus
{
    Task Because() => Plan();

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_profile_refusal() => _result.Diagnostics.Where(_ => ProfileCodes.Contains(_.Code)).ShouldBeEmpty();

    static readonly string[] ProfileCodes =
    [
        SceneProfileResolution.UnknownPackageCode,
        SceneProfileResolution.MissingLayoutCode,
        SceneProfileResolution.IncompatiblePackageCode,
        SceneProfileResolution.MissingThemeCode
    ];
}
