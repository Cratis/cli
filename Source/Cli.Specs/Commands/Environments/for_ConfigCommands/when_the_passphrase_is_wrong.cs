// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments.for_ConfigCommands;

[Collection(CliSpecsCollection.Name)]
public class when_the_passphrase_is_wrong : given.an_environment_project
{
    const string SecretValue = "wrong-passphrase-secret";

    async Task Because()
    {
        await Init("production");
        await SetSecret("smtp:password", SecretValue, "production");
        Environment.SetEnvironmentVariable("CRATIS_PASSPHRASE", "not the passphrase");
        await Run(new ConfigGetCommand(), new ConfigGetSettings { Key = "smtp:password", ShowSecret = true, EnvironmentShort = "production" });
    }

    [Fact] void should_fail_with_the_authentication_exit_code() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_print_nothing_on_stdout() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_explain_on_stderr() => _error.ToString().ShouldContain("passphrase is incorrect");
    [Fact] void should_not_leak_the_value() => AllOutput.ShouldNotContain(SecretValue);
}
