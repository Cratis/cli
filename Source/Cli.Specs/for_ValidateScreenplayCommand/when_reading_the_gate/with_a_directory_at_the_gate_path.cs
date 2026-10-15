// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_reading_the_gate;

public class with_a_directory_at_the_gate_path : given.a_validation_gate_cli_process
{
    void Establish() => Directory.CreateDirectory(_gatePath);
    Task Because() => Run();
    [Fact] void should_refuse_input() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_the_location() => _error.ShouldContain("screenplay.json:1:1:");
    [Fact] void should_explain_the_expected_file() => _error.ShouldContain("regular file");
}
