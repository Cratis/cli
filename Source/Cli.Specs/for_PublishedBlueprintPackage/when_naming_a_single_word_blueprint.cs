// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PublishedBlueprintPackage;

public class when_naming_a_single_word_blueprint : Specification
{
    string? _result;

    void Because() => _result = PublishedBlueprintPackage.NpmPackageFor("Cratis.Blueprint.Invoicing");

    [Fact] void should_name_the_kebab_cased_npm_package() => _result.ShouldEqual("@cratis/scene.blueprint.invoicing");
}
