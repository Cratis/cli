// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.SampleData;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class an_explicit_pack : given.a_sample_pack
{
    void Establish() => _scene = _scene with
    {
        SampleDataPacks = [.. _scene.SampleDataPacks!, new("chosen", "Chosen", "en", "user", [new("Products", [new JsonObject { ["id"] = "chosen-1", ["name"] = "Chosen" }])])],
        UiProfiles = [_scene.UiProfiles[0] with { SampleData = new([], [new SampleDataBinding("Products", "demo")], "demo") }]
    };

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, "chosen");

    [Fact] void should_override_the_binding_and_active_pack() => _seed.ShouldContain("name = \"Chosen\"");
    [Fact] void should_not_use_the_bound_pack() => _seed.ShouldNotContain("name = \"Mug\"");
}
