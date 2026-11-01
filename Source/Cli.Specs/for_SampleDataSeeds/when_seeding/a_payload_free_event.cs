// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_payload_free_event : given.a_sample_pack
{
    void Establish()
    {
        _row.Clear();
        _row["id"] = "product-1";
        _application = Application(
            [new("Drafted", [], Location)],
            [new("Products", [Property("id")], Location)],
            [From("Drafted", Set("id", new EventSourceIdExpressionSyntax(Location)))]);
    }

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_emit_the_payload_free_event() => _seed.ShouldContain("    Drafted");
    [Fact] void should_use_the_mapped_event_source_id() => _seed.ShouldContain("for \"product-1\"");
    [Fact] void should_not_warn() => _warnings.ShouldBeEmpty();
    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
