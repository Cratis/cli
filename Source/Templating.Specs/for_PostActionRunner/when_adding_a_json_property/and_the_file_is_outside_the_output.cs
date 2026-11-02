// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_a_json_property;

public class and_the_file_is_outside_the_output : Specification
{
    const string Original = "{\"theme\":\"dark\"}";
    string _outputRoot;
    string _outsideFile;
    IReadOnlyList<PostActionResult> _results;

    void Establish()
    {
        var parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-json", Guid.NewGuid().ToString("N"))).FullName;
        _outputRoot = Directory.CreateDirectory(Path.Combine(parent, "app")).FullName;
        _outsideFile = Path.Combine(parent, "settings.json");
        File.WriteAllText(_outsideFile, Original);
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
                    ActionId = "695A3659-EB40-4FF5-A6A6-C9C4E629FCB0",
                    Args = new Dictionary<string, string>
                    {
                        ["jsonFileName"] = Path.Combine("..", "settings.json"),
                        ["newJsonPropertyName"] = "injected",
                        ["newJsonPropertyValue"] = "true"
                    }
                }
            ]
        },
        new InstantiationResult("MyApp", _outputRoot, [], [], new Dictionary<string, string>(), []),
        new Dictionary<string, string>());

    [Fact] void should_fail_the_action() => _results[0].Outcome.ShouldEqual(PostActionOutcome.Failed);
    [Fact] void should_leave_the_outside_file_unchanged() => File.ReadAllText(_outsideFile).ShouldEqual(Original);
}
