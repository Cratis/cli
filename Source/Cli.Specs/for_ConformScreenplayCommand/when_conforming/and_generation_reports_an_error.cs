// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_generation_reports_an_error : given.a_conform_command
{
    void Establish() => Generated(Source, new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "TEST001", "Could not recover source", null));
    async Task Because() => await Execute();
    [Fact] void should_report_that_the_check_could_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_write_generation_diagnostics_to_standard_error() => _error.ShouldContain("TEST001");
    [Fact] void should_not_claim_a_comparison_verdict() => _output.ValueKind.ShouldEqual(System.Text.Json.JsonValueKind.Undefined);
}
