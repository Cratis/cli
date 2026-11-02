// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_to_a_solution;

public class and_the_solution_file_is_outside_the_output : Specification
{
    string _outputRoot;
    string _outsideSolution;
    IReadOnlyList<PostActionResult> _results;

    void Establish()
    {
        var parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-solutions", Guid.NewGuid().ToString("N"))).FullName;
        _outputRoot = Directory.CreateDirectory(Path.Combine(parent, "app")).FullName;
        _outsideSolution = Path.Combine(parent, "Other.slnx");
        File.WriteAllText(Path.Combine(_outputRoot, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    async Task Because() => _results = await new PostActionRunner().Run(
        new TemplateConfig
        {
            Name = "MyApp",
            ShortName = "t",
            PostActions =
            [
                new PostActionConfig
                {
                    ActionId = "D396686C-DE0E-4DE6-906D-291CD29FC5DE",
                    Args = new Dictionary<string, string> { ["solutionFile"] = _outsideSolution }
                }
            ]
        },
        new InstantiationResult("MyApp", _outputRoot, [], [Path.Combine(_outputRoot, "App.csproj")], new Dictionary<string, string>(), []),
        new Dictionary<string, string>());

    [Fact] void should_fail_the_action() => _results[0].Outcome.ShouldEqual(PostActionOutcome.Failed);
    [Fact] void should_not_create_the_outside_solution() => File.Exists(_outsideSolution).ShouldBeFalse();
}
