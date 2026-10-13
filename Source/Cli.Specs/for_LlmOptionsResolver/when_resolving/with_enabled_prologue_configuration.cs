// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_enabled_prologue_configuration : Specification
{
    LlmOptions _result;

    void Because() => _result = LlmOptionsResolver.ResolveWithSource(
        """{"llm":{"enabled":true,"kind":"Ollama","modelId":"gemma"}}""",
        new LlmConfiguration { Kind = "anthropic", ApiKey = "sk-ant-key" }).Options;

    [Fact] void should_be_enabled() => _result.Enabled.ShouldBeTrue();
    [Fact] void should_use_the_local_provider() => _result.Kind.ShouldEqual(LlmKind.Ollama);
    [Fact] void should_use_the_local_model() => _result.ModelId.ShouldEqual("gemma");
}
