// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_openai_cli_configuration : Specification
{
    LlmOptions _result;

    void Because() => _result = LlmOptionsResolver.Resolve(
        null,
        new LlmConfiguration { Kind = "openai", ApiKey = "sk-key" });

    [Fact] void should_be_enabled() => _result.Enabled.ShouldBeTrue();
    [Fact] void should_map_to_the_openai_kind() => _result.Kind.ShouldEqual(LlmKind.OpenAI);
    [Fact] void should_leave_the_model_empty_for_the_provider_default() => _result.ModelId.ShouldEqual(string.Empty);
    [Fact] void should_keep_the_default_endpoint() => _result.Endpoint.ShouldEqual(new LlmOptions().Endpoint);
}
