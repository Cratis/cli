// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments.for_ConfigCommands;

[Collection(CliSpecsCollection.Name)]
public class when_a_named_secret_is_an_argument : given.an_environment_project
{
    const string SecretValue = "typed-after-a-named-secret";

    string _file = string.Empty;

    async Task Because()
    {
        await Init("production");
        await Run(new ConfigSetCommand(), new ConfigSetSettings { Key = "secrets.StripeKey", Value = SecretValue, EnvironmentShort = "production" });
        _file = EnvironmentFile("production");
    }

    [Fact] void should_refuse_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_repeat_the_value() => AllOutput.ShouldNotContain(SecretValue);
    [Fact] void should_not_store_it() => _file.ShouldNotContain(SecretValue);
}
