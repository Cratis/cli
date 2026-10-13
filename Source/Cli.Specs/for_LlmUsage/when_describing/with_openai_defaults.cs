// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;
using Cratis.Prologue.Interpretation;

namespace Cratis.Cli.for_LlmUsage.when_describing;

public class with_openai_defaults : Specification
{
    LlmUsage _result;

    void Because() => _result = LlmUsage.From(new LlmOptions { Enabled = true, Kind = LlmKind.OpenAI, ModelId = string.Empty, Endpoint = "https://ignored.example/secret" }, "global config");

    [Fact] void should_name_the_default_model() => _result.Model.ShouldEqual(LlmChatClient.DefaultOpenAIModelId);
    [Fact] void should_name_the_actual_public_endpoint() => _result.EndpointHost.ShouldEqual("api.openai.com");
}
