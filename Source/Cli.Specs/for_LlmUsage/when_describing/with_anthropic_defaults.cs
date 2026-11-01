// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;
using Cratis.Prologue.Interpretation;

namespace Cratis.Cli.for_LlmUsage.when_describing;

[Collection(CliSpecsCollection.Name)]
public class with_anthropic_defaults : Specification
{
    string? _previousEndpoint;
    LlmUsage _result;

    void Establish()
    {
        _previousEndpoint = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL");
        Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", null);
    }

    void Because() => _result = LlmUsage.From(new LlmOptions { Enabled = true, Kind = LlmKind.Anthropic, ModelId = string.Empty }, "global config");

    [Fact] void should_name_the_default_model() => _result.Model.ShouldEqual(LlmChatClient.DefaultAnthropicModelId);
    [Fact] void should_name_the_actual_public_endpoint() => _result.EndpointHost.ShouldEqual("api.anthropic.com");

    void Destroy() => Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", _previousEndpoint);
}
