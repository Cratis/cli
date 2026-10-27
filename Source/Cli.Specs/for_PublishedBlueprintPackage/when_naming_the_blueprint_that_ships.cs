// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PublishedBlueprintPackage;

public class when_naming_the_blueprint_that_ships : Specification
{
    string? _result;

    void Because() => _result = PublishedBlueprintPackage.NpmPackageFor("Cratis.Blueprint.Default");

    [Fact] void should_name_no_package() => _result.ShouldBeNull();
}
