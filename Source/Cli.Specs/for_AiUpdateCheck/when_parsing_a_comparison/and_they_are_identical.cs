// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_parsing_a_comparison;

public class and_they_are_identical : Specification
{
    AiCorpusUpdate? _result;

    void Because() => _result = AiUpdateCheck.ParseComparison("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", """{"status":"identical","ahead_by":0,"behind_by":0}""");

    [Fact] void should_find_no_new_commits() => _result!.NewCommits.ShouldEqual(0);
}
