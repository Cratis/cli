// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetResolver.when_resolving;

public class and_the_path_is_a_solution_filter : given.a_temporary_folder
{
    string _filter;
    ScreenplayTarget _result;

    void Establish()
    {
        _filter = Path.Combine(_folder, "MyApp.slnf");
        File.WriteAllText(_filter, "{}");
    }

    void Because() => _result = ScreenplayTargetResolver.Resolve("MyApp.slnf", _folder);

    [Fact] void should_resolve() => _result.IsResolved.ShouldBeTrue();
    [Fact] void should_resolve_the_filter() => _result.Path.ShouldEqual(_filter);
    [Fact] void should_read_it_as_the_solution_it_filters() => ScreenplayTargetResolver.IsSolution(_filter).ShouldBeTrue();
}
