// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_only_elixir_and_python_are_selected : given.a_corpus_with_language_rules
{
    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis/example"], ["elixir", "python"]));

    [Fact] void should_install_the_mixed_language_rule() => File.Exists(Installed("mixed.md")).ShouldBeTrue();
    [Fact] void should_install_the_python_rule() => File.Exists(Installed("python.md")).ShouldBeTrue();
    [Fact] void should_install_the_rule_with_braced_extensions() => File.Exists(Installed("braced.md")).ShouldBeTrue();
}
