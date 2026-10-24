// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class an_unknown_pack : given.a_sample_pack
{
    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, "missing");

    [Fact] void should_refuse_the_named_pack() => _error.ShouldContain("Sample data pack 'missing' was not found");
    [Fact] void should_not_emit_seeds() => _seed.ShouldBeNull();
}
