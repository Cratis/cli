// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmUsage.when_describing;

[Collection(CliSpecsCollection.Name)]
public class with_an_openai_environment_endpoint : Specification
{
    string? _previousEndpoint;
    LlmUsage _result;

    void Establish()
    {
        _previousEndpoint = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        Environment.SetEnvironmentVariable("OPENAI_BASE_URL", "https://environment.example");
    }

    void Because() => _result = LlmUsage.From(new LlmOptions { Enabled = true, Kind = LlmKind.OpenAI }, "global config");

    [Fact] void should_match_the_clients_public_endpoint() => _result.EndpointHost.ShouldEqual("api.openai.com");

    void Destroy() => Environment.SetEnvironmentVariable("OPENAI_BASE_URL", _previousEndpoint);
}
