// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.SampleData;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_bound_pack : given.a_sample_pack
{
    void Establish() => _scene = _scene with
    {
        SampleDataPacks = [.. _scene.SampleDataPacks, new("bound", "Bound", "en", "user", [new("Products", [new JsonObject { ["id"] = "bound-1", ["name"] = "Bound" }])])],
        UiProfiles = [_scene.UiProfiles[0] with { SampleData = new([], [new SampleDataBinding("Products", "bound")], "demo") }]
    };

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_prefer_the_binding_to_the_active_pack() => _seed.ShouldContain("name = \"Bound\"");
    [Fact] void should_not_seed_the_active_pack_as_well() => _seed.ShouldNotContain("name = \"Mug\"");
}
