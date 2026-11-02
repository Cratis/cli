// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_only_python_is_selected : given.a_corpus_with_language_rules
{
    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis/example"], ["python"]));

    [Fact] void should_install_the_python_rule() => File.Exists(Installed("python.md")).ShouldBeTrue();
    [Fact] void should_not_install_the_mixed_language_rule() => File.Exists(Installed("mixed.md")).ShouldBeFalse();
    [Fact] void should_install_a_rule_whose_brace_groups_include_python() => File.Exists(Installed("groups.md")).ShouldBeTrue();
}
