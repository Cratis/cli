// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Integration.Chronicle.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_renaming_context_to_same_name : Specification
{
    CliCommandResult _createResult = null!;
    CliCommandResult _renameResult = null!;

    async Task Because()
    {
        _createResult = await CliCommandRunner.RunAsync(
            "context",
            "create",
            "integration-test-same-name",
            "--server",
            "chronicle://localhost:35000/?disableTls=true");

        _renameResult = await CliCommandRunner.RunAsync("context", "rename", "integration-test-same-name", "integration-test-same-name", "--output", "json");

        // Clean up.
        await CliCommandRunner.RunAsync("context", "delete", "integration-test-same-name");
    }

    [Fact] void should_create_successfully() => _createResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_return_validation_error_exit_code() => _renameResult.ExitCode.ShouldEqual(ExitCodes.ValidationError);

    [Fact] void should_report_error_on_stderr() => _renameResult.StandardError.ShouldContain("validation_error");
}
