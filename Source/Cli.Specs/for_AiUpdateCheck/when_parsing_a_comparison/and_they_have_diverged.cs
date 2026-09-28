// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_parsing_a_comparison;

/// <summary>
/// Installed from another branch - 'cratis ai update' would not simply bring in newer commits.
/// </summary>
public class and_they_have_diverged : Specification
{
    AiCorpusUpdate? _result;

    void Because() => _result = AiUpdateCheck.ParseComparison("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", """{"status":"diverged","ahead_by":5,"behind_by":2}""");

    [Fact] void should_find_no_new_commits() => _result!.NewCommits.ShouldEqual(0);
}
