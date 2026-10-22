// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_render_admission;

public class with_a_stage_refusal : given.a_stage_refused_model
{
    async Task Because() => await Run("--executable", "--target", "cratis", "--name", "Projects");

    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_stage_refusal() => _error.ShouldContain("STAGE-ESM-006");
    [Fact] void should_report_the_play_location() => Diagnostic.GetProperty("location").GetString().ShouldContain("MyApp.play(");
    [Fact] void should_keep_the_semantic_identity() => Diagnostic.GetProperty("subject").GetString().StartsWith("sem1:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_write_nothing() => Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(_document)!).Count().ShouldEqual(1);

    JsonElement Diagnostic => JsonDocument.Parse(_error).RootElement.GetProperty("diagnostics")
        .EnumerateArray().First(diagnostic => diagnostic.GetProperty("code").GetString() == "STAGE-ESM-006");
}
