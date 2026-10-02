// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ViewTarget.when_resolving;

public class with_a_folder_without_a_project : given.a_temporary_folder
{
    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "Nested"));
        File.WriteAllText(Path.Combine(_folder, "Nested", "Library.csproj"), "<Project />");
    }

    void Because() => _result = ViewTarget.Resolve(_folder);

    [Fact] void should_not_search_beneath_the_folder() => _result.ProjectFile.ShouldBeNull();
    [Fact] void should_say_there_is_no_project() => _result.Error!.ShouldContain("holds no C# project file");
}
