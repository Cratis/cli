// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;
using Microsoft.Extensions.AI;

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_local_enabled : given.an_interpret_command
{
    void Establish() => Configure("""{"llm":{"enabled":true,"kind":"OpenAICompatible","modelId":"project-model","endpoint":"https://user:password@project.example/v1?secret=hidden","accessToken":"local-secret"}}""");
    Task Because() => Interpret();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_use_the_local_provider() => _chatClients.Received(1).CreateFor(Arg.Is<LlmOptions>(options => options.ModelId == "project-model"));
    [Fact] void should_call_the_model() => _client.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_announce_before_creating_the_client() => _noticeBeforeClient.ShouldContain("OpenAICompatible; model: project-model; endpoint host: project.example; source: local file");
    [Fact] void should_announce_before_sending_evidence() => _noticeBeforeRequest.ShouldContain("Capture evidence will be sent to this provider.");
    [Fact] void should_include_the_model_in_json() => _llm.GetProperty("model").GetString().ShouldEqual("project-model");
    [Fact] void should_include_only_the_endpoint_host_in_json() => _llm.GetProperty("endpointHost").GetString().ShouldEqual("project.example");
    [Fact] void should_not_expose_the_access_token() => _output.ShouldNotContain("local-secret");
    [Fact] void should_not_expose_endpoint_credentials() => _notice.ShouldNotContain("password");
    [Fact] void should_not_expose_endpoint_query_strings() => _notice.ShouldNotContain("hidden");
}
