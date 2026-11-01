// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;
using Microsoft.Extensions.AI;

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_an_invalid_endpoint : given.an_interpret_command
{
    [Theory]
    [InlineData("127.0.0.1:11434")]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("file:///private/captures")]
    [InlineData("https://user:secret@")]
    public async Task should_reject_before_creating_a_client_or_sending_evidence(string endpoint)
    {
        Configure(System.Text.Json.JsonSerializer.Serialize(new { llm = new { enabled = true, kind = "Ollama", endpoint } }));

        await Interpret();

        _exitCode.ShouldEqual(ExitCodes.ValidationError);
        _notice.ShouldContain("absolute HTTP or HTTPS URL with a host");
        _notice.ShouldNotContain("secret");
        _chatClients.DidNotReceive().CreateFor(Arg.Any<LlmOptions>());
        await _client.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
        File.Exists(_settings.File).ShouldBeFalse();
    }
}
