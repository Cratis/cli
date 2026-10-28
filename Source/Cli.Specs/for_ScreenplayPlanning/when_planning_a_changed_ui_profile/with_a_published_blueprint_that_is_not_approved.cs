// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

public class with_a_published_blueprint_that_is_not_approved : given.the_screen_composition_corpus
{
    void Establish() => ChangeApplication("^( +)Cratis\\.Components$", "$1Cratis.Components\n$1Cratis.Blueprint.ProjectTracker");

    Task Because() => Plan();

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_name_the_npm_package() => ErrorsWith(SceneProfileResolution.UnknownPackageCode).Single().Message.ShouldContain("@cratis/scene.blueprint.project-tracker");
    [Fact] void should_point_to_the_documentation() => ErrorsWith(SceneProfileResolution.UnknownPackageCode).Single().Message.ShouldContain("#use-a-published-scene-blueprint");
}
