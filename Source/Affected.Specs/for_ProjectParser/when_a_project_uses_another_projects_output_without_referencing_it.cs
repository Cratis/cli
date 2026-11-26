// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_ProjectParser;

public class when_a_project_uses_another_projects_output_without_referencing_it : Specification
{
    ProjectGraph _graph;
    IReadOnlyList<string> _owners;

    void Because()
    {
        var configuration = new AffectedConfiguration
        {
            ImplicitReferences = new Dictionary<string, IReadOnlyList<string>> { ["Source/Storage.Specs/**"] = ["Source/Server/Server.csproj"] }
        };
        var projects = new[]
        {
            ProjectParser.Parse("Source/Server/Server.csproj", "<Project/>", configuration),
            ProjectParser.Parse("Source/Storage.Specs/Storage.Specs.csproj", "<Project><PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup></Project>", configuration),
            ProjectParser.Parse("Source/Other.Specs/Other.Specs.csproj", "<Project><PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup></Project>", configuration)
        };
        _graph = new ProjectGraph(projects);
        _owners = [.. _graph.Projects["Source/Storage.Specs/Storage.Specs.csproj"].References];
    }

    [Fact] void should_treat_the_output_as_a_reference() => _owners.ShouldContainOnly("Source/Server/Server.csproj");
    [Fact] void should_leave_other_projects_alone() => _graph.Projects["Source/Other.Specs/Other.Specs.csproj"].References.ShouldBeEmpty();
}
