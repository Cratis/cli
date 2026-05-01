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
}
