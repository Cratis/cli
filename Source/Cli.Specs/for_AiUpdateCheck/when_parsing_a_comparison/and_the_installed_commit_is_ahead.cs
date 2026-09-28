// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_parsing_a_comparison;

/// <summary>
/// Installed from a local checkout with commits not yet on the default branch.
/// </summary>
public class and_the_installed_commit_is_ahead : Specification
{
    AiCorpusUpdate? _result;

    void Because() => _result = AiUpdateCheck.ParseComparison("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", """{"status":"behind","ahead_by":0,"behind_by":3}""");

    [Fact] void should_find_no_new_commits() => _result!.NewCommits.ShouldEqual(0);
}
