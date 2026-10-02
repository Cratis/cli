// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ViewTarget.when_resolving;

public class with_a_folder_holding_several_projects : given.a_temporary_folder
{
    void Establish()
    {
        Create("Library.csproj");
        Create("Library.Specs.csproj");
    }

    void Because() => _result = ViewTarget.Resolve(_folder);

    [Fact] void should_not_guess_a_project() => _result.ProjectFile.ShouldBeNull();
    [Fact] void should_ask_for_the_project_to_be_named() => _result.Error!.ShouldContain("name the one to view");
}
