// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Integration.Chronicle.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_deleting_context : Specification
{
    CliCommandResult _createResult = null!;
    CliCommandResult _deleteResult = null!;

    void Establish()
    {
        // Creating a context makes it the active one when no other context is active, and the active context
        // cannot be deleted. Make another context active first so the outcome does not depend on what an
        // earlier specification in the shared collection left behind.
        var config = CliConfiguration.Load();
        if (!config.Contexts.ContainsKey(CliConfiguration.DefaultContextName))
        {
            config.Contexts[CliConfiguration.DefaultContextName] = new CliContext();
        }

        config.ActiveContext = CliConfiguration.DefaultContextName;
        config.Save();
    }

    async Task Because()
    {
        _createResult = await CliCommandRunner.RunAsync(
            "context",
            "create",
            "integration-test-del",
            "--server",
            "chronicle://localhost:35000/?skipTlsValidation=true");

        _deleteResult = await CliCommandRunner.RunAsync("context", "delete", "integration-test-del", "--output", "json", "--yes");
    }

    [Fact] void should_create_successfully() => _createResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_delete_successfully() => _deleteResult.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_no_errors_on_delete() => _deleteResult.StandardError.ShouldEqual(string.Empty);
}
