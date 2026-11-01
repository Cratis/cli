// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.Commands.Environments.for_ConfigCommands;

[Collection(CliSpecsCollection.Name)]
public class when_changing_the_passphrase_and_the_provider : given.an_environment_project
{
    const string SecretValue = "moves-with-the-passphrase";

    string _saltBefore = string.Empty;
    string _saltAfter = string.Empty;
    int _changeResult;
    string _read = string.Empty;
    int _unsupportedResult;

    async Task Because()
    {
        await Init("production");
        await SetSecret("smtp:password", SecretValue, "production");
        _saltBefore = EnvironmentFile("production").Split('\n').Single(line => line.StartsWith("encryptionSalt:", StringComparison.Ordinal));

        Environment.SetEnvironmentVariable("CRATIS_NEW_PASSPHRASE", "a new passphrase");
        _changeResult = await Run(new EnvChangePassphraseCommand(), new EnvironmentSettings { EnvironmentShort = "production" });
        _saltAfter = EnvironmentFile("production").Split('\n').Single(line => line.StartsWith("encryptionSalt:", StringComparison.Ordinal));

        Environment.SetEnvironmentVariable("CRATIS_PASSPHRASE", "a new passphrase");
        await Run(new ConfigGetCommand(), new ConfigGetSettings { Key = "smtp:password", ShowSecret = true, EnvironmentShort = "production" });
        _read = JsonDocument.Parse(_output.ToString()).RootElement.GetProperty("value").GetString()!;

        _unsupportedResult = await Run(new EnvChangeSecretsProviderCommand(), new EnvChangeSecretsProviderSettings { Provider = "plaintext", EnvironmentShort = "production" });
    }

    [Fact] void should_change_the_passphrase() => _changeResult.ShouldEqual(ExitCodes.Success);
    [Fact] void should_use_a_new_salt() => _saltAfter.ShouldNotEqual(_saltBefore);
    [Fact] void should_still_read_the_secret_with_the_new_passphrase() => _read.ShouldEqual(SecretValue);
    [Fact] void should_refuse_an_unknown_provider() => _unsupportedResult.ShouldEqual(ExitCodes.ValidationError);
}
