// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_building_the_hint;

public class and_an_update_is_available : Specification
{
    string _result = null!;

    void Because() => _result = AiUpdateCheck.GetUpdateHint(new AiCorpusUpdate("1234567aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 12));

    [Fact] void should_count_the_new_commits_since_the_shortened_revision() => _result.ShouldContain("12 new commits since 1234567");
    [Fact] void should_tell_how_to_update() => _result.ShouldContain("run 'cratis ai update'");
}
