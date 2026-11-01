// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_collection : given.a_sample_pack
{
    void Establish() => _application = Application(
        [new("Drafted", [Property("name")], Location)],
        [new("Products", [Property("name"), Property("lines", collection: true)], Location)],
        [From("Drafted", Set("name", new PathExpressionSyntax("name", Location))),
         new ChildrenSyntax("lines", new PathExpressionSyntax("sku", Location), AutoMapMode.Disabled, [], Location)]);

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_seed_the_scalar_fields() => _seed.ShouldContain("name = \"Mug\"");
    [Fact] void should_warn_once_per_collection_property() => _warnings.Count.ShouldEqual(1);
    [Fact] void should_name_the_upstream_limitation() => _warnings.Single().Message.ShouldContain("Products.lines stays empty: sample data cannot give rows to a collection property yet (Cratis/Screenplay#1000)");
}
