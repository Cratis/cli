// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ViewTarget.when_resolving;

public class with_a_project_file : given.a_temporary_folder
{
    string _project;

    void Establish()
    {
        _project = Create("Library.csproj");
        Create("Other.csproj");
    }

    void Because() => _result = ViewTarget.Resolve(_project);

    [Fact] void should_resolve_the_named_project() => _result.ProjectFile.ShouldEqual(_project);
}
