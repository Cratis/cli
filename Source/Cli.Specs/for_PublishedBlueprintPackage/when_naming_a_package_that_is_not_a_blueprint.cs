// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PublishedBlueprintPackage;

public class when_naming_a_package_that_is_not_a_blueprint : Specification
{
    string? _result;

    void Because() => _result = PublishedBlueprintPackage.NpmPackageFor("Acme.Charts");

    [Fact] void should_name_no_package() => _result.ShouldBeNull();
}
