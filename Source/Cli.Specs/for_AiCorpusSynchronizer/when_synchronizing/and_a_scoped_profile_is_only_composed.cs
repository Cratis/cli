// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_scoped_profile_is_only_composed : given.a_corpus_with_profile_scoped_rules
{
    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis"], []));

    [Fact] void should_not_install_the_scoped_rule() => File.Exists(Installed("scoped.md")).ShouldBeFalse();
    [Fact] void should_install_the_rule_without_a_profile() => File.Exists(Installed("general.md")).ShouldBeTrue();
}
