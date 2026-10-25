// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class without_an_event_source_mapping : given.a_sample_pack
{
    string? _again;

    void Establish() => _application = Application(
        [new("Drafted", [Property("name")], Location)],
        [new("Products", [Property("name")], Location)],
        [From("Drafted", Set("name", new PathExpressionSyntax("name", Location)))]);

    void Because()
    {
        (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);
        _again = SampleDataSeeds.Create(_application, _scene, null).SeedPlay;
    }

    [Fact] void should_derive_a_stable_id() => _seed.ShouldEqual(_again);
    [Fact] void should_use_a_guid() => Guid.TryParse(_seed!.Split('"')[3], out _).ShouldBeTrue();
    [Fact] void should_not_invent_an_event_source_mapping() => _seed.ShouldNotContain("product-1");
}
