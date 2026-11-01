// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_an_anthropic_environment_endpoint : given.an_interpret_command
{
    [Theory]
    [InlineData(null, "https://user:secret@environment.example/path?token=hidden", "environment.example")]
    [InlineData("", "https://environment.example", "environment.example")]
    [InlineData("http://llm:11434", "https://environment.example", "environment.example")]
    [InlineData("https://user:secret@configured.example/path?token=hidden", "https://environment.example", "configured.example")]
    [InlineData(null, "http://llm:11434", "llm")]
    public async Task should_disclose_and_pin_the_effective_endpoint(string? endpoint, string environmentEndpoint, string expectedHost)
    {
        var previousEndpoint = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL");
        try
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", environmentEndpoint);
            _global = new() { Kind = "anthropic", Endpoint = endpoint, Model = "claude-opus-4-6" };

            await Interpret();

            _exitCode.ShouldEqual(ExitCodes.Success);
            _noticeBeforeClient.ShouldContain($"endpoint host: {expectedHost}");
            _noticeBeforeRequest.ShouldContain($"endpoint host: {expectedHost}");
            _llm.GetProperty("endpointHost").GetString().ShouldEqual(expectedHost);
            _chatClients.Received(1).CreateFor(Arg.Is<LlmOptions>(options => options.Endpoint == new Uri(string.IsNullOrEmpty(endpoint) || endpoint == "http://llm:11434" ? environmentEndpoint : endpoint).AbsoluteUri));
            _notice.ShouldNotContain("secret");
            _notice.ShouldNotContain("hidden");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_BASE_URL", previousEndpoint);
        }
    }
}
