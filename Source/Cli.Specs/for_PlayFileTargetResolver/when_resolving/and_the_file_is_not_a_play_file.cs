// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFileTargetResolver.when_resolving;

public class and_the_file_is_not_a_play_file : given.a_temporary_folder
{
    ScreenplayTarget _result;

    void Establish() => File.WriteAllText(Path.Combine(_folder, "readme.md"), "not a Screenplay");

    void Because() => _result = PlayFileTargetResolver.Resolve("readme.md", _folder);

    [Fact] void should_not_resolve() => _result.IsResolved.ShouldBeFalse();
    [Fact] void should_report_that_it_is_not_a_play_file() => _result.Error.ShouldContain("is not a Screenplay (.play) file");
    [Fact] void should_suggest_what_to_point_at() => _result.Suggestion.ShouldNotBeNull();
}
