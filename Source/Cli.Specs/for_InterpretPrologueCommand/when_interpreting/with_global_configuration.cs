// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_global_configuration : given.an_interpret_command
{
    Task Because() => Interpret();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_announce_the_global_provider_before_sending_evidence() => _noticeBeforeRequest.ShouldContain("OpenAI; model: global-model; endpoint host: api.openai.com; source: global config");
    [Fact] void should_report_the_provider_in_json() => _llm.GetProperty("kind").GetString().ShouldEqual("OpenAI");
    [Fact] void should_report_the_global_source_in_json() => _llm.GetProperty("source").GetString().ShouldEqual("global config");
    [Fact] void should_report_refinement_enabled() => _llm.GetProperty("used").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_expose_the_global_secret() => _notice.ShouldNotContain("global-secret");
}
