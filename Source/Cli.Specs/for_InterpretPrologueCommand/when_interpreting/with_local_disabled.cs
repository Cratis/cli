// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_local_disabled : given.an_interpret_command
{
    void Establish() => Configure("""{"llm":{"enabled":false}}""");
    Task Because() => Interpret();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_write_a_screenplay() => File.ReadAllText(_settings.File).ShouldNotBeEmpty();
    [Fact] void should_not_create_a_chat_client() => _chatClients.DidNotReceive().CreateFor(Arg.Any<LlmOptions>());
    [Fact] void should_report_no_model_in_json() => _llm.GetProperty("used").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_local_source() => _llm.GetProperty("source").GetString().ShouldEqual("local file");
    [Fact] void should_report_heuristics_only() => _notice.ShouldContain("Interpreting with heuristics only.");
}
