// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class later_events : given.a_sample_pack
{
    void Establish() => _application = Application(
        [new("Drafted", [Property("name")], Location), new("Priced", [Property("price")], Location), new("Approved", [], Location)],
        [new("Products", [Property("id"), Property("name"), Property("price"), Property("status")], Location)],
        [From("Drafted", Set("id", new EventSourceIdExpressionSyntax(Location)), Set("name", new PathExpressionSyntax("name", Location)), Set("status", new LiteralExpressionSyntax("draft", Location))),
         From("Priced", Set("price", new PathExpressionSyntax("price", Location))),
         From("Approved", Set("status", new LiteralExpressionSyntax("approved", Location)))]);

    void Because()
    {
        _row["status"] = "approved";
        (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);
    }

    [Fact] void should_create_before_updating() => (_seed!.IndexOf("Drafted", StringComparison.Ordinal) < _seed.IndexOf("Priced", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_supply_the_later_event() => _seed.ShouldContain("price = 49");
    [Fact] void should_choose_a_matching_constant() => _seed.ShouldContain("Approved");
    [Fact] void should_not_warn_about_a_reproducible_constant() => _warnings.ShouldBeEmpty();
}
