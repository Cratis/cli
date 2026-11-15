// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.Commands.Environments.for_ConfigCommands;

[Collection(CliSpecsCollection.Name)]
public class when_setting_and_reading_secrets : given.an_environment_project
{
    const string SecretValue = "very-secret-value-42";

    string _setOutput = string.Empty;
    string _listOutput = string.Empty;
    string _getOutput = string.Empty;
    string _shownOutput = string.Empty;
    string _file = string.Empty;
    int _setResult;
    int _getResult;
    int _shownResult;

    async Task Because()
    {
        await Init("production");
        await SetPlain("auth:clientId", "client-1", "production");
        _setResult = await SetSecret("smtp:password", SecretValue, "production");
        _setOutput = AllOutput;
        await Run(new ConfigListCommand(), new ConfigListSettings { EnvironmentShort = "production" });
        _listOutput = AllOutput;
        _getResult = await Run(new ConfigGetCommand(), new ConfigGetSettings { Key = "smtp:password", EnvironmentShort = "production" });
        _getOutput = AllOutput;
        _shownResult = await Run(new ConfigGetCommand(), new ConfigGetSettings { Key = "smtp:password", ShowSecret = true, EnvironmentShort = "production" });
        _shownOutput = AllOutput;
        _file = EnvironmentFile("production");
    }

    [Fact] void should_store_the_secret() => _setResult.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_echo_the_secret_when_setting_it() => _setOutput.ShouldNotContain(SecretValue);
    [Fact] void should_not_list_the_secret() => _listOutput.ShouldNotContain(SecretValue);
    [Fact] void should_show_a_placeholder_in_the_list() => _listOutput.ShouldContain("[secret]");
    [Fact] void should_list_plain_values() => _listOutput.ShouldContain("client-1");
    [Fact] void should_succeed_when_getting_without_show_secret() => _getResult.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_print_the_secret_without_show_secret() => _getOutput.ShouldNotContain(SecretValue);
    [Fact] void should_print_the_secret_with_show_secret() => JsonElement.Parse(_shownOutput).GetProperty("value").GetString().ShouldEqual(SecretValue);
    [Fact] void should_succeed_with_show_secret() => _shownResult.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_write_the_secret_to_the_file() => _file.ShouldNotContain(SecretValue);
}
