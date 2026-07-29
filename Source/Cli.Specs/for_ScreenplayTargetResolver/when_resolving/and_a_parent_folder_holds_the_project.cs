// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetResolver.when_resolving;

public class and_a_parent_folder_holds_the_project : given.a_temporary_folder
{
    string _project;
    string _nested;
    ScreenplayTarget _result;

    void Establish()
    {
        _project = Path.Combine(_folder, "MyApp.csproj");
        File.WriteAllText(_project, "<Project />");
        _nested = Directory.CreateDirectory(Path.Combine(_folder, "Features", "Authors")).FullName;
    }

    void Because() => _result = ScreenplayTargetResolver.Resolve(null, _nested);

    [Fact] void should_find_the_project_by_searching_upwards() => _result.Path.ShouldEqual(_project);
}
