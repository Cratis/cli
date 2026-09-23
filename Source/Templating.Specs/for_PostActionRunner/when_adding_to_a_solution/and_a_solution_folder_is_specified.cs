// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_to_a_solution;

public class and_a_solution_folder_is_specified : Specification
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
            PostActions =
            [
                new PostActionConfig
                {
                    ActionId = "D396686C-DE0E-4DE6-906D-291CD29FC5DE",
                    Args = new Dictionary<string, string> { ["solutionFolder"] = "src" }
                }
            ]
        },
        new InstantiationResult("MyApp", _outputRoot, [], [Path.Combine(_outputRoot, "App.csproj")], new Dictionary<string, string>(), []),
        new Dictionary<string, string>());

    [Fact] void should_record_the_folder_on_the_project_entry() =>
        File.ReadAllText(Path.Combine(_outputRoot, "MyApp.slnx")).ShouldContain("Folder=\"src\"");
}
