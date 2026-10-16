// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

/// <summary>
/// A version pin the installed default blueprint does not satisfy stops the render with a diagnostic naming the template and the pin,
/// instead of rendering an application in a shell other than the one it asked for (Cratis/Stage#466).
/// </summary>
public class with_a_root_template_pin_no_blueprint_satisfies : given.the_screen_composition_corpus
{
    void Establish()
    {
        ChangeApplication("^( +)root template AppShell$", "$1root template blueprint Cratis.Blueprint.Default AppShell version \"^99.0\"");
        ChangeApplication("^( +)Cratis\\.Components$", "$1Cratis.Components\n$1Cratis.Blueprint.Default");
    }

    Task Because() => Plan();

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_root_template() => ErrorsWith(SceneProfileResolution.RootTemplateCode).Single().Message.ShouldContain("AppShell");
}
