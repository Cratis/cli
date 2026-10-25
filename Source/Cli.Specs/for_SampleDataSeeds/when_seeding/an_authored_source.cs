// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class an_authored_source : given.a_sample_pack
{
    void Establish() => _application = _application with
    {
        Seeds = [new SeedSyntax([new("product-1", [new("Drafted", [], Location)], Location)], Location)]
    };

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_not_duplicate_the_authored_seed() => _seed.ShouldBeNull();
    [Fact] void should_explain_which_seed_wins() => _warnings.Single().Message.ShouldContain("authored seed wins");
}
