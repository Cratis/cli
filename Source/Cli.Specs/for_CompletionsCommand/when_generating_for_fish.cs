// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_generating_for_fish : Specification
{
    string _result;

    void Because() => _result = FishCompletionGenerator.Generate();

    [Fact] void should_contain_complete_command() => _result.ShouldContain("complete -c cratis");
    [Fact] void should_contain_chronicle_completion() => _result.ShouldContain("'chronicle'");
    [Fact] void should_contain_subcommand_condition() => _result.ShouldContain("__fish_seen_subcommand_from");
    [Fact] void should_pass_the_current_word_for_ai_profiles() => _result.ShouldContain("_complete ai-profiles --current (commandline -ct | string collect)");
    [Fact] void should_pass_the_current_word_for_llm_kind() => _result.ShouldContain("_complete llm-kinds --current (commandline -ct | string collect)");
    [Fact] void should_pass_the_current_word_for_output_format() => _result.ShouldContain("_complete output-formats --current (commandline -ct | string collect)");
}
