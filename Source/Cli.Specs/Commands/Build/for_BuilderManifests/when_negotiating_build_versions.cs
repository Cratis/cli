// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuilderManifests;

public class when_negotiating_build_versions : Specification
{
    string[] _host;
    string[] _defaultKit;
    string[] _explicitKit;

    void Because()
    {
        _host = Versions(BuilderManifests.For(null, "generator"));
        _defaultKit = Versions(BuilderManifests.For(new BuildKit("unused", []), "generator"));
        _explicitKit = Versions(BuilderManifests.For(new BuildKit("unused", new() { ["contract"] = new JsonObject { ["versions"] = new JsonArray("2", "unsupported") } }), "generator"));
    }

    [Fact] void should_not_offer_removals_for_a_kitless_generator() => string.Join(',', _host).ShouldEqual("1,2");
    [Fact] void should_offer_removals_for_the_packaged_builder() => string.Join(',', Versions(BuilderManifests.For(null, "x", BuildLevel.AI, "cratis"))).ShouldEqual("1,2,3");
    [Fact] void should_leave_unspecified_kits_on_one() => string.Join(',', _defaultKit).ShouldEqual("1");
    [Fact] void should_intersect_explicit_kit_support() => string.Join(',', _explicitKit).ShouldEqual("2");

    static string[] Versions(JsonObject manifest) => [.. manifest["contractVersions"]!.AsArray().Select(version => version!.GetValue<string>())];
}
