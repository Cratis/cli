// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InterpretPrologueCommand.when_interpreting;

[Collection(CliSpecsCollection.Name)]
public class with_an_unknown_captured_service : given.an_interpret_command
{
    void Establish()
    {
        _settings.NoLlm = true;
        _settings.Service = "shipping";
    }

    Task Because() => Interpret();

    [Fact] void should_report_not_found() => _exitCode.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_not_write_a_screenplay() => File.Exists(_settings.File).ShouldBeFalse();
}
