// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_scoped_profile_is_no_longer_selected : given.a_corpus_with_profile_scoped_rules
{
    SyncResult _result = null!;

    void Establish() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis/example"], []));

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis"], []));

    [Fact] void should_remove_the_scoped_rule() => File.Exists(Installed("scoped.md")).ShouldBeFalse();
    [Fact] void should_report_the_removal() => _result.Actions.ShouldContain("Removed rules/scoped.md");
    [Fact] void should_preserve_the_rule_without_a_profile() => File.Exists(Installed("general.md")).ShouldBeTrue();
    [Fact] void should_not_report_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
