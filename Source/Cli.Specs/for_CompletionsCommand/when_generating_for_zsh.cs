// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_generating_for_zsh : Specification
{
    string _result;

    void Because() => _result = ZshCompletionGenerator.Generate();

    [Fact] void should_start_with_compdef() => Assert.StartsWith("#compdef cratis", _result);
    [Fact] void should_contain_function_definition() => _result.ShouldContain("_cratis()");
    [Fact] void should_contain_describe_commands() => _result.ShouldContain("_describe");
    [Fact] void should_contain_chronicle_description() => _result.ShouldContain("chronicle");
}
