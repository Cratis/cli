// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Integration.Chronicle.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_deleting_default_context : Specification
{
    CliCommandResult _result = null!;

    void Establish()
    {
        // The default context exists only once a configuration has been saved; without this the outcome
        // depends on whether an earlier specification happened to save one.
        var config = CliConfiguration.Load();
        if (!config.Contexts.ContainsKey(CliConfiguration.DefaultContextName))
        {
            config.Contexts[CliConfiguration.DefaultContextName] = new CliContext();
            config.Save();
        }
    }

    async Task Because() => _result = await CliCommandRunner.RunAsync("context", "delete", "default", "--output", "json");

    [Fact] void should_return_validation_error_exit_code() => _result.ExitCode.ShouldEqual(ExitCodes.ValidationError);

    [Fact] void should_report_error_on_stderr() => _result.StandardError.ShouldContain("validation_error");
}
