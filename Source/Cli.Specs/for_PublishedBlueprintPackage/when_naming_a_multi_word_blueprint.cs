// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PublishedBlueprintPackage;

public class when_naming_a_multi_word_blueprint : Specification
{
    string? _result;

    void Because() => _result = PublishedBlueprintPackage.NpmPackageFor("Cratis.Blueprint.ProjectTracker");

    [Fact] void should_separate_the_words_with_a_dash() => _result.ShouldEqual("@cratis/scene.blueprint.project-tracker");
}
