// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_plain_output : given.an_interpret_command
{
    void Establish() => _settings.Output = OutputFormats.Plain;
    Task Because() => Interpret();

    [Fact] void should_include_the_provider_in_the_result() => _output.ShouldContain("Language model: OpenAI; model: global-model; endpoint host: api.openai.com; source: global config");
    [Fact] void should_announce_before_sending_evidence() => _noticeBeforeRequest.ShouldContain("source: global config");
}
