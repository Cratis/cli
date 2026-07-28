// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetResolver.when_resolving;

public class and_the_path_is_a_project_file : given.a_temporary_folder
{
    string _project;
    ScreenplayTarget _result;

    void Establish()
    {
        _project = Path.Combine(_folder, "MyApp.csproj");
        File.WriteAllText(_project, "<Project />");
    }

    void Because() => _result = ScreenplayTargetResolver.Resolve("MyApp.csproj", _folder);

    [Fact] void should_resolve() => _result.IsResolved.ShouldBeTrue();
    [Fact] void should_resolve_the_project_relative_to_the_current_directory() => _result.Path.ShouldEqual(_project);
    [Fact] void should_not_report_an_error() => _result.Error.ShouldBeNull();
}
