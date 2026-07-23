// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_anthropic_cli_configuration : Specification
{
    LlmOptions _result;

    void Because() => _result = LlmOptionsResolver.Resolve(
        null,
        new LlmConfiguration { Kind = "anthropic", ApiKey = "sk-ant-key", Model = "claude-opus-4-6" });

    [Fact] void should_be_enabled() => _result.Enabled.ShouldBeTrue();
    [Fact] void should_map_to_the_anthropic_kind() => _result.Kind.ShouldEqual(LlmKind.Anthropic);
    [Fact] void should_carry_the_api_key_as_access_token() => _result.AccessToken.ShouldEqual("sk-ant-key");
    [Fact] void should_carry_the_model() => _result.ModelId.ShouldEqual("claude-opus-4-6");
}
