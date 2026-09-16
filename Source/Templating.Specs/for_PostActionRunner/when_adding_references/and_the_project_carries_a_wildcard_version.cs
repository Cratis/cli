// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Configuration;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_references;

public class and_the_project_carries_a_wildcard_version : given_a_project_to_finish
{
    string _project = null!;
    IReadOnlyList<PostActionResult> _results = null!;

    void Establish() => _project = WriteProject(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Some.Package\" Version=\"*\" />\n  </ItemGroup>\n</Project>");

    async Task Because() => _results = await new PostActionRunner().Run(
        new TemplateConfig { Name = "T", ShortName = "t", PostActions = [PackageReferenceAction("Some.Package")] },
        ResultFor("App.csproj"),
        new Dictionary<string, string>());

    [Fact] void should_succeed() => _results[0].Outcome.ShouldEqual(PostActionOutcome.Succeeded);

    [Fact] void should_resolve_the_wildcard_from_the_local_feed() =>
        File.ReadAllText(_project).ShouldContain("Include=\"Some.Package\" Version=\"3.2.1\"");

    [Fact] void should_not_leave_a_wildcard_in_the_project() =>
        File.ReadAllText(_project).ShouldNotContain("Version=\"*\"");
}
