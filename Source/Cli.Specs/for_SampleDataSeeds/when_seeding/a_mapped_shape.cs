// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.SampleData;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_mapped_shape : given.a_sample_pack
{
    void Establish() => _scene = _scene with
    {
        SampleDataPacks = [new("demo", "Demo", "en", "user", [new("Cards", [_row])])],
        UiProfiles = [_scene.UiProfiles[0] with { SampleData = new([new SampleDataMapping("Cards", "Products")], [], "demo") }]
    };

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_seed_the_mapped_read_model() => _seed.ShouldContain("name = \"Mug\"");
    [Fact] void should_use_the_mapped_event_source_id() => _seed.ShouldContain("for \"product-1\"");
    [Fact] void should_name_the_pack() => _seed.ShouldContain("seed \"Sample data demo\"");
    [Fact] void should_not_refuse() => _error.ShouldBeNull();
}
