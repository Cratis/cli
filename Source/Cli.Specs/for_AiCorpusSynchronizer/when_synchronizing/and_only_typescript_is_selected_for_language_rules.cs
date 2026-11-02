// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_only_typescript_is_selected_for_language_rules : given.a_corpus_with_language_rules
{
    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new([], ["cratis/example"], ["typescript"]));

    [Fact] void should_install_a_rule_whose_nested_alternatives_include_typescript() => File.Exists(Installed("nested.md")).ShouldBeTrue();
    [Fact] void should_not_install_a_rule_whose_full_pattern_alternatives_exclude_typescript() => File.Exists(Installed("alternatives.md")).ShouldBeFalse();
    [Fact] void should_keep_a_rule_with_an_unmatched_closing_brace() => File.Exists(Installed("unmatched-closing.md")).ShouldBeTrue();
    [Fact] void should_keep_a_rule_with_an_unmatched_opening_brace() => File.Exists(Installed("unmatched-opening.md")).ShouldBeTrue();
}
