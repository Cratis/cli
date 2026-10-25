// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_blueprint_pack : given.a_sample_pack
{
    void Establish() => _scene = _scene with
    {
        UiProfiles = [_scene.UiProfiles[0] with { SampleData = new([], [], "blueprint-demo") }],
        BlueprintSampleDataPacks = ["blueprint-demo"]
    };

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_not_seed_rows_it_does_not_have() => _seed.ShouldBeNull();
    [Fact] void should_warn_about_blueprint_packs() => _warnings.Single().Message.ShouldContain("Blueprint sample data pack 'blueprint-demo' is not seeded yet");
    [Fact] void should_not_refuse_the_run() => _error.ShouldBeNull();
}
