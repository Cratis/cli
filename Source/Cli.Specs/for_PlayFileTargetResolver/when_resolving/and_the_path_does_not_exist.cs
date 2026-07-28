// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFileTargetResolver.when_resolving;

public class and_the_path_does_not_exist : given.a_temporary_folder
{
    ScreenplayTarget _result;

    void Because() => _result = PlayFileTargetResolver.Resolve("Missing/MyApp.play", _folder);

    [Fact] void should_not_resolve() => _result.IsResolved.ShouldBeFalse();
    [Fact] void should_report_that_it_does_not_exist() => _result.Error.ShouldContain("does not exist");
    [Fact] void should_suggest_what_to_point_at() => _result.Suggestion.ShouldNotBeNull();
}
