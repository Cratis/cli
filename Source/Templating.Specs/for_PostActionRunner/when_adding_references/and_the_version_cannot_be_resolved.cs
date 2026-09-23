// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Configuration;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_references;

public class and_the_version_cannot_be_resolved : given_a_project_to_finish
{
    IReadOnlyList<PostActionResult> _results = null!;

    void Establish() => WriteProject(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Missing.Package\" Version=\"*\" />\n  </ItemGroup>\n</Project>");

    async Task Because() => _results = await new PostActionRunner().Run(
        new TemplateConfig
        {
            Name = "T",
            ShortName = "t",
            PostActions = [PackageReferenceAction("Missing.Package") with { ContinueOnError = false }]
        },
        ResultFor("App.csproj"),
        new Dictionary<string, string>());

    [Fact] void should_fail_the_action() => _results[0].Outcome.ShouldEqual(PostActionOutcome.Failed);

    [Fact] void should_count_a_non_continue_on_error_failure_against_the_run() => _results[0].IsFailure.ShouldBeTrue();

    [Fact] void should_report_the_wildcard_project_as_a_failed_scaffold() =>
        _results[0].Message.ShouldContain("Missing.Package");
}
