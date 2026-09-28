// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_building_the_hint;

public class and_there_is_one_new_commit : Specification
{
    string _result = null!;

    void Because() => _result = AiUpdateCheck.GetUpdateHint(new AiCorpusUpdate("1234567aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 1));

    [Fact] void should_use_the_singular() => _result.ShouldContain("1 new commit since");
    [Fact] void should_tell_how_to_update() => _result.ShouldContain("run 'cratis ai update'");
}
