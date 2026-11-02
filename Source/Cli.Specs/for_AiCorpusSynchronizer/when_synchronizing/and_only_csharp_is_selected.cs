// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_only_csharp_is_selected : given.a_corpus_with_language_rules
{
    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis/example"], ["csharp"]));

    [Fact] void should_not_treat_a_stylesheet_rule_as_csharp() => File.Exists(Installed("styles.md")).ShouldBeTrue();
    [Fact] void should_install_the_mixed_language_rule() => File.Exists(Installed("mixed.md")).ShouldBeTrue();
    [Fact] void should_install_a_rule_whose_full_pattern_alternatives_include_csharp() => File.Exists(Installed("alternatives.md")).ShouldBeTrue();
    [Fact] void should_install_a_rule_whose_nested_alternatives_include_csharp() => File.Exists(Installed("nested.md")).ShouldBeTrue();
    [Fact] void should_not_install_a_rule_whose_brace_groups_target_other_languages() => File.Exists(Installed("groups.md")).ShouldBeFalse();
    [Fact] void should_install_a_scripting_rule_with_language_independent_patterns() => File.Exists(Installed("scripting.md")).ShouldBeTrue();
    [Fact] void should_install_a_rule_that_also_targets_every_file() => File.Exists(Installed("everything.md")).ShouldBeTrue();
}
