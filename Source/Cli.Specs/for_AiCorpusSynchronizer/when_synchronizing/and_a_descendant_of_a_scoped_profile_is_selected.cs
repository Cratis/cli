// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_descendant_of_a_scoped_profile_is_selected : given.a_corpus_with_profile_scoped_rules
{
    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis/example/child"], []));

    [Fact] void should_install_the_scoped_rule() => File.Exists(Installed("scoped.md")).ShouldBeTrue();
}
