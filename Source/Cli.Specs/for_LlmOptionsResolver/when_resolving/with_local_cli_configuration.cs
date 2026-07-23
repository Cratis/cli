// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_local_cli_configuration : Specification
{
    LlmOptions _result;

    void Because() => _result = LlmOptionsResolver.Resolve(
        null,
        new LlmConfiguration { Kind = "local", Endpoint = "http://localhost:11434/v1", Model = "llama3" });

    [Fact] void should_be_enabled() => _result.Enabled.ShouldBeTrue();
    [Fact] void should_map_to_the_openai_compatible_kind() => _result.Kind.ShouldEqual(LlmKind.OpenAICompatible);
    [Fact] void should_carry_the_endpoint() => _result.Endpoint.ShouldEqual("http://localhost:11434/v1");
    [Fact] void should_carry_the_model() => _result.ModelId.ShouldEqual("llama3");
}
