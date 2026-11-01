// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_required_event_collection : given.a_sample_pack
{
    void Establish() => _application = Application(
        [new("Drafted", [Property("name"), Property("lines", collection: true)], Location)],
        [new("Products", [Property("name")], Location)],
        [From("Drafted", Set("name", new PathExpressionSyntax("name", Location)))]);

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_skip_the_row_instead_of_emitting_an_invalid_event() => _seed.ShouldBeNull();
    [Fact] void should_name_the_required_collection() => _warnings.Single().Message.ShouldContain("Drafted.lines");
    [Fact] void should_name_the_upstream_limitation() => _warnings.Single().Message.ShouldContain("Cratis/Screenplay#1000");
}
