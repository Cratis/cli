// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_disabled_prologue_configuration_and_cli_configuration : Specification
{
    LlmOptions _result;

    void Because() => _result = LlmOptionsResolver.Resolve(
        new PrologueConfiguration { Llm = new LlmOptions { Enabled = false } },
        new LlmConfiguration { Kind = "anthropic", ApiKey = "sk-ant-key" });

    [Fact] void should_fall_back_to_the_cli_configuration() => _result.Kind.ShouldEqual(LlmKind.Anthropic);
    [Fact] void should_be_enabled() => _result.Enabled.ShouldBeTrue();
}
