// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments.for_ConfigCommands;

[Collection(CliSpecsCollection.Name)]
public class when_listing_missing_keys : given.an_environment_project
{
    int _missingResult;
    string _missingOutput = string.Empty;
    int _completeResult;
    int _unreadableResult;

    async Task Because()
    {
        await Init("production");
        await SetPlain("auth:clientId", "client-1", "production");
        await File.WriteAllTextAsync(Path.Combine(_root, "keys.txt"), "# keys the model needs\nauth:clientId\nsmtp:host\n\nsmtp:password\n");
        _missingResult = await Run(new ConfigListCommand(), new ConfigListSettings { Missing = "keys.txt", EnvironmentShort = "production" });
        _missingOutput = _output.ToString();

        await File.WriteAllTextAsync(Path.Combine(_root, "present.json"), "[\"auth:clientId\"]");
        _completeResult = await Run(new ConfigListCommand(), new ConfigListSettings { Missing = "present.json", EnvironmentShort = "production" });

        await File.WriteAllTextAsync(Path.Combine(_root, "model.play"), "feature X");
        _unreadableResult = await Run(new ConfigListCommand(), new ConfigListSettings { Missing = "model.play", EnvironmentShort = "production" });
    }

    [Fact] void should_report_missing_keys_with_the_validation_exit_code() => _missingResult.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_list_each_missing_key() => _missingOutput.ShouldContain("smtp:host");
    [Fact] void should_list_the_other_missing_key() => _missingOutput.ShouldContain("smtp:password");
    [Fact] void should_not_list_present_keys() => _missingOutput.ShouldNotContain("auth:clientId");
    [Fact] void should_succeed_when_nothing_is_missing() => _completeResult.ShouldEqual(ExitCodes.Success);
    [Fact] void should_explain_when_a_model_cannot_be_read_yet() => _unreadableResult.ShouldEqual(ExitCodes.ValidationError);
}
