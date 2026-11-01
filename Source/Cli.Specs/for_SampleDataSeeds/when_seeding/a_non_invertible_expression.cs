// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_non_invertible_expression : given.a_sample_pack
{
    void Establish() => _application = Application(
        [new("Drafted", [Property("name")], Location)],
        [new("Products", [Property("name"), Property("label")], Location)],
        [From("Drafted", Set("name", new PathExpressionSyntax("name", Location)), Set("label", new PathExpressionSyntax("product.label", Location)))]);

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_keep_the_invertible_scalar() => _seed.ShouldContain("name = \"Mug\"");
    [Fact] void should_warn_about_the_unsupported_expression() => _warnings.Single().Message.ShouldContain("Products.label has a non-invertible projection expression");
}
