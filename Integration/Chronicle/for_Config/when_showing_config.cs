// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Integration.Chronicle.for_Config;

[Collection(ChronicleCollection.Name)]
public class when_showing_config : Specification
{
    CliCommandResult _result = null!;

    async Task Because() => _result = await CliCommandRunner.RunAsync("config", "show", "--output", "json");

    [Fact] void should_return_success_exit_code() => _result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_output_json() => _result.StandardOutput.Contains('{').ShouldBeTrue();

    [Fact] void should_have_no_errors() => _result.StandardError.ShouldEqual(string.Empty);
}
