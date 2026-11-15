// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_to_a_solution;

public class and_a_discovered_solution_is_behind_a_link_outside : Specification
{
    const string OutsideContent = "<Solution>\n</Solution>\n";
    string _outputRoot;
    string _outsideSolution;
    bool _linked;
    IReadOnlyList<PostActionResult> _results;

    void Establish()
    {
        var parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-solutions", Guid.NewGuid().ToString("N"))).FullName;
        _outputRoot = Directory.CreateDirectory(Path.Combine(parent, "app")).FullName;
        var outside = Directory.CreateDirectory(Path.Combine(parent, "elsewhere")).FullName;
        _outsideSolution = Path.Combine(outside, "Other.slnx");
        File.WriteAllText(_outsideSolution, OutsideContent);
        File.WriteAllText(Path.Combine(_outputRoot, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        try
        {
            File.CreateSymbolicLink(Path.Combine(_outputRoot, "App.slnx"), _outsideSolution);
            _linked = true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _linked = false;
        }
    }

    async Task Because()
    {
        if (!_linked) return;
        _results = await new PostActionRunner().Run(
            new TemplateConfig
            {
                Name = "MyApp",
                ShortName = "t",
                PostActions = [new PostActionConfig { ActionId = "D396686C-DE0E-4DE6-906D-291CD29FC5DE" }]
            },
            new InstantiationResult("MyApp", _outputRoot, [], [Path.Combine(_outputRoot, "App.csproj")], new Dictionary<string, string>(), []),
            new Dictionary<string, string>());
    }

    [Fact]
    void should_leave_the_outside_solution_untouched()
    {
        if (!_linked) return;
        File.ReadAllText(_outsideSolution).ShouldEqual(OutsideContent);
    }

    [Fact]
    void should_add_the_project_to_a_solution_inside_the_output()
    {
        if (!_linked) return;
        File.ReadAllText(Path.Combine(_outputRoot, "MyApp.slnx")).ShouldContain("App.csproj");
    }
}
