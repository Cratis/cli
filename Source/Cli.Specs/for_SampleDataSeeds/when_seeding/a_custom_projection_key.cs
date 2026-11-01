// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_custom_projection_key : given.a_sample_pack
{
    void Establish() => _application = Application(
        [new("Drafted", [Property("name")], Location)],
        [new("Products", [Property("name")], Location)],
        [new FromSyntax([new("Drafted", null, Location)], new ExpressionKeySyntax(new PathExpressionSyntax("sku", Location), Location), null, [Set("name", new PathExpressionSyntax("name", Location))], Location)]);

    void Because() => (_seed, _warnings, _error) = SampleDataSeeds.Create(_application, _scene, null);

    [Fact] void should_not_guess_the_key() => _seed.ShouldBeNull();
    [Fact] void should_explain_the_key_limitation() => _warnings.ShouldContain(warning => warning.Message.Contains("uses a projection key", StringComparison.Ordinal));
}
