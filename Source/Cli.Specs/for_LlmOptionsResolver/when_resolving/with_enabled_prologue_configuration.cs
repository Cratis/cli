// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_enabled_prologue_configuration : Specification
{
    PrologueConfiguration _configuration;
    LlmOptions _result;

    void Establish() => _configuration = new PrologueConfiguration
    {
        Llm = new LlmOptions { Enabled = true, Kind = LlmKind.Ollama, ModelId = "gemma" }
    };

    void Because() => _result = LlmOptionsResolver.Resolve(
        _configuration,
        new LlmConfiguration { Kind = "anthropic", ApiKey = "sk-ant-key" });

    [Fact] void should_use_the_prologue_configuration() => _result.ShouldEqual(_configuration.Llm);
}
