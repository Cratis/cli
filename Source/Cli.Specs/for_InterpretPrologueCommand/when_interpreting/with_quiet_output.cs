// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_quiet_output : given.an_interpret_command
{
    void Establish()
    {
        _settings.Quiet = true;
        _settings.Output = OutputFormats.Plain;
    }
    Task Because() => Interpret();

    [Fact] void should_announce_before_sending_evidence() => _noticeBeforeRequest.ShouldContain("source: global config");
    [Fact] void should_keep_stdout_to_the_output_path() => _output.Trim().ShouldEqual(_settings.File);
}
