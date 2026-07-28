// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayTargetResolver.when_resolving;

public class and_nothing_can_be_discovered : given.a_temporary_folder
{
    ScreenplayTarget _result;

    void Because() => _result = ScreenplayTargetResolver.Resolve(_folder, _folder);

    [Fact] void should_not_resolve() => _result.IsResolved.ShouldBeFalse();
    [Fact] void should_report_that_nothing_was_found() => _result.Error.ShouldContain("No solution or project file found");
    [Fact] void should_suggest_passing_a_path() => _result.Suggestion.ShouldContain("pass the path to one");
}
