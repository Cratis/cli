// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_required_unmapped_property : given.a_sample_pack
{
    void Establish() => _application = Application(
        [new("Drafted", [Property("name"), Property("currency")], Location)],
        [new("Products", [Property("name")], Location)],
        [From("Drafted", Set("name", new PathExpressionSyntax("name", Location)))]);

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_not_guess_a_required_event_value() => _seed.ShouldBeNull();
    [Fact] void should_explain_why_the_row_was_skipped() => _warnings.Single().Message.ShouldContain("required event property 'Drafted.currency' cannot be supplied");
}
