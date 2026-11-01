// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.for_SampleDataSeeds.when_seeding;

public class a_round_trip : Specification
{
    const string Customer = "A \"quoted\" name\\with a slash\nand a new line";
    string _source;
    string _diagnostics;
    string? _restored;

    void Establish() => _source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shell", "model", "application.play"));

    void Because()
    {
        var compiler = new PlayFileCompiler();
        var application = compiler.CompileFolder([new PlayFileContent("application.play", _source)]).Result.Value!;
        var scene = new ScreenplaySceneVisitor().Visit(application);
        scene.SampleDataPacks![0].Sets[0].Rows[0]["customer"] = Customer;
        var seeds = SampleDataSeeds.Create(application, scene, null);
        var result = compiler.CompileFolder([new PlayFileContent("model/application.play", _source), new PlayFileContent("cratis-sample-data.play", seeds.SeedPlay!)]).Result;
        _diagnostics = string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message));
        _restored = ((LiteralExpressionSyntax?)result.Value?.Seeds?.Single().Groups.Single().Events.Single().Properties.Single(property => property.Property == "customer").Source)?.Value as string;
    }

    [Fact] void should_compile_the_original_model_and_sibling_seed() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_quotes_backslashes_and_line_breaks() => _restored.ShouldEqual(Customer);
}
