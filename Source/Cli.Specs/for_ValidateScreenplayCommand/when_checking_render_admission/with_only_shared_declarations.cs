// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_render_admission;

public class with_only_shared_declarations : given.a_completeness_cli_process
{
    int _renderExitCode;

    void Establish() => File.WriteAllText(_document, "concept ProjectId : Uuid\n");

    async Task Because()
    {
        await RunCommand("render", _document, "--check", "--name", "App", "--destination", Path.Combine(Path.GetDirectoryName(_document)!, "out"), "-o", _format);
        _renderExitCode = _exitCode;
        await Run("--executable", "--target", "cratis", "--name", "App");
    }

    [Fact] void should_admit_render_check_with_changes_pending() => _renderExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_pass_target_validation() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_admission_as_passed()
    {
        using var document = JsonDocument.Parse(_output);
        document.RootElement.GetProperty("targetAdmission").GetProperty("status").GetString().ShouldEqual("passed");
    }
    [Fact] void should_not_report_an_empty_selection() => _error.ShouldNotContain("STAGE-PLAN-010");
    [Fact] void should_write_nothing() => Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(_document)!).Count().ShouldEqual(1);
}
