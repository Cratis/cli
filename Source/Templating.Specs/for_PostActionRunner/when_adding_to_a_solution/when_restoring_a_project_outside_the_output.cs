// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_to_a_solution;

public class when_restoring_a_project_outside_the_output : Specification
{
    PostActionResult? _result;
    string _outputRoot;
    string _outsideProject;

    void Establish()
    {
        var parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-restore", Guid.NewGuid().ToString("N"))).FullName;
        _outputRoot = Directory.CreateDirectory(Path.Combine(parent, "app")).FullName;
        _outsideProject = Path.Combine(parent, "Victim.csproj");
        File.WriteAllText(_outsideProject, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    async Task Because() => _result = await Restore.RunForSpecs(
        new PostActionConfig { ActionId = "210D431B-A78B-4D2F-B762-4ED3E3EA9025" },
        new InstantiationResult(Path.Combine("..", "Victim"), _outputRoot, [], [_outsideProject], new Dictionary<string, string>(), []),
        isDotnetAvailable: () => true);

    [Fact] void should_not_restore_any_project() => _result.Message.ShouldContain("restored 0 project(s)");
}
