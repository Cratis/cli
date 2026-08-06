// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetResolver.when_resolving;

public class and_the_folder_holds_a_solution_and_a_filter : given.a_temporary_folder
{
    string _solution;
    ScreenplayTarget _result;

    void Establish()
    {
        _solution = Path.Combine(_folder, "MyApp.slnx");
        File.WriteAllText(_solution, "<Solution />");
        File.WriteAllText(Path.Combine(_folder, "MyApp.slnf"), "{}");
    }

    void Because() => _result = ScreenplayTargetResolver.Resolve(null, _folder);

    [Fact] void should_prefer_the_whole_solution_over_one_view_of_it() => _result.Path.ShouldEqual(_solution);
}
