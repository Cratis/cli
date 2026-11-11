// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_baseline_warnings_are_reported_in_json : given.a_baseline_workspace
{
    string _withoutBaseline = null!;

    async Task Because()
    {
        _output = await Capture();
        _settings.Baseline = null;
        _withoutBaseline = (await Capture()).Stdout;
    }

    [Fact] void should_return_success() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_stage_warning_in_json_on_stderr() => JsonElement.Parse(_output.Stderr).GetProperty("diagnostics").EnumerateArray().Any(diagnostic => diagnostic.GetProperty("code").GetString() == "STAGE-BASELINE-001").ShouldBeTrue();
    [Fact] void should_keep_warnings_out_of_stdout() => _output.Stdout.ShouldNotContain("STAGE-BASELINE-");
    [Fact] void should_keep_the_publication_receipt_shape() => JsonElement.Parse(_output.Stdout).EnumerateObject().Select(property => property.Name).ShouldEqual(JsonElement.Parse(_withoutBaseline).EnumerateObject().Select(property => property.Name));
    [Fact] void should_report_no_diagnostics_in_the_receipt() => JsonElement.Parse(_output.Stdout).GetProperty("publication").TryGetProperty("diagnostics", out _).ShouldBeFalse();
}
