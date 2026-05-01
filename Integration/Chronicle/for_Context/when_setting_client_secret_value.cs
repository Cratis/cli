// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Integration.Chronicle.for_Context;

[Collection(ChronicleCollection.Name)]
public class when_setting_client_secret_value : Specification
{
    CliCommandResult _result = null!;
    string _originalSecret = null!;

    async Task Because()
    {
        _originalSecret = "super-secret-value";
        _result = await CliCommandRunner.RunAsync("context", "set-value", "client-secret", _originalSecret, "--output", "json");
    }

    [Fact] void should_return_success_exit_code() => _result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_no_errors() => _result.StandardError.ShouldEqual(string.Empty);

    [Fact] void should_not_expose_secret_in_output() => _result.StandardOutput.ShouldNotContain(_originalSecret);

    [Fact] void should_mask_secret_in_output() => _result.StandardOutput.ShouldContain("********");
}
