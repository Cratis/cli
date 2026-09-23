// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_generating_for_bash : Specification
{
    string _result;

    void Because() => _result = BashCompletionGenerator.Generate();

    [Fact] void should_contain_function_definition() => _result.ShouldContain("_cratis()");
    [Fact] void should_contain_complete_registration() => _result.ShouldContain("complete -F _cratis cratis");
    [Fact] void should_contain_chronicle_subcommand() => _result.ShouldContain("chronicle)");
    [Fact] void should_contain_compgen() => _result.ShouldContain("compgen -W");
    [Fact] void should_complete_ai_profile_values_with_current_word() => _result.ShouldContain("_complete ai-profiles --current \"$cur\"");
    [Fact] void should_complete_comma_separated_harnesses() => _result.ShouldContain("_complete ai-harnesses --current \"$cur\"");
    [Fact] void should_complete_llm_kinds_and_models() => _result.ShouldContain("_complete llm-kinds --current \"$cur\"");
    [Fact] void should_complete_output_formats_offline() => _result.ShouldContain("_complete output-formats --current \"$cur\"");
}
