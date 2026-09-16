// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_to_a_solution;

public class and_no_solution_exists : Specification
{
    string _outputRoot = null!;
    IReadOnlyList<PostActionResult> _results = null!;

    void Establish()
    {
        _outputRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-solutions", Guid.NewGuid().ToString("N"))).FullName;
        File.WriteAllText(Path.Combine(_outputRoot, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    async Task Because() => _results = await new PostActionRunner().Run(
        new TemplateConfig
        {
            Name = "MyApp",
            ShortName = "t",
            PostActions = [new PostActionConfig { ActionId = "D396686C-DE0E-4DE6-906D-291CD29FC5DE" }]
        },
        new InstantiationResult("MyApp", _outputRoot, [], [Path.Combine(_outputRoot, "App.csproj")], new Dictionary<string, string>(), []),
        new Dictionary<string, string>());

    [Fact] void should_succeed() => _results[0].Outcome.ShouldEqual(PostActionOutcome.Succeeded);

    [Fact] void should_create_an_slnx_referencing_the_project() =>
        File.ReadAllText(Path.Combine(_outputRoot, "MyApp.slnx")).ShouldContain("App.csproj");
}
