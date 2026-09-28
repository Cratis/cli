// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_parsing_a_comparison;

/// <summary>
/// GitHub describes the head (the default branch) relative to the base (the installed commit).
/// </summary>
public class and_the_default_branch_is_ahead : Specification
{
    AiCorpusUpdate? _result;

    void Because() => _result = AiUpdateCheck.ParseComparison("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", """{"status":"ahead","ahead_by":12,"behind_by":0,"total_commits":12}""");

    [Fact] void should_count_the_new_commits() => _result!.NewCommits.ShouldEqual(12);
    [Fact] void should_keep_the_installed_revision() => _result!.InstalledRevision.ShouldEqual("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
}
