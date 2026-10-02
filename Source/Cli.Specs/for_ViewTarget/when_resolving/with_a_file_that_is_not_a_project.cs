// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ViewTarget.when_resolving;

public class with_a_file_that_is_not_a_project : given.a_temporary_folder
{
    void Because() => _result = ViewTarget.Resolve(Create("Library.sln"));

    [Fact] void should_not_resolve_a_project() => _result.ProjectFile.ShouldBeNull();
    [Fact] void should_say_it_is_not_a_project() => _result.Error!.ShouldContain("is not a C# project file");
}
