// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_a_missing_baseline_file : given.a_baseline_workspace
{
    void Establish()
    {
        File.Delete(_baselinePath);
        _settings.Check = true;
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_keep_the_publication_exit_code() => _output.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_report_unreadable_warning() => _output.Stderr.ShouldContain("STAGE-BASELINE-003");
    [Fact] void should_explain_the_missing_file() => JsonDocument.Parse(_output.Stderr).RootElement.GetProperty("diagnostics").EnumerateArray().Single(diagnostic => diagnostic.GetProperty("code").GetString() == "STAGE-BASELINE-003").GetProperty("message").GetString()!.ShouldContain($"the release baseline file '{_baselinePath}' does not exist");
    [Fact] void should_not_write_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
}
