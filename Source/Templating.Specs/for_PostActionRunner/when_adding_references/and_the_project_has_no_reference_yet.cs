// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Configuration;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_references;

public class and_the_project_has_no_reference_yet : given_a_project_to_finish
{
    string _project = null!;

    void Establish() => _project = WriteProject(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Other\" Version=\"1.0.0\" />\n  </ItemGroup>\n</Project>");

    async Task Because() => await new PostActionRunner().Run(
        new TemplateConfig { Name = "T", ShortName = "t", PostActions = [PackageReferenceAction("Some.Package")] },
        ResultFor("App.csproj"),
        new Dictionary<string, string>());

    [Fact] void should_insert_the_reference_into_the_existing_item_group() =>
        File.ReadAllText(_project).ShouldContain("Include=\"Some.Package\" Version=\"3.2.1\"");

    [Fact] void should_keep_the_existing_references() =>
        File.ReadAllText(_project).ShouldContain("Include=\"Other\" Version=\"1.0.0\"");
}
