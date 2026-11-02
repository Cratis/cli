// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_PassphraseSecretsProvider;

public class when_the_passphrase_is_wrong : a_temp_project
{
    Exception _error;
    string _resolved;

    async Task Because()
    {
        var environment = ProjectEnvironment.Create(_environments, "production", null, PassphraseSources.Fixed("correct horse"), FastKdf);
        await environment.SetAsync("smtp:password", "value", secret: true);

        var wrong = ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("battery staple"));
        _error = await Catch.Exception(async () => _resolved = await wrong.GetValueAsync("smtp:password"));
    }

    [Fact] void should_fail_with_an_invalid_passphrase_error() => _error.ShouldBeOfExactType<InvalidPassphraseException>();
    [Fact] void should_not_return_partial_output() => _resolved.ShouldBeNull();
    [Fact] void should_say_how_to_provide_the_passphrase() => _error.Message.ShouldContain("CRATIS_PASSPHRASE");
}
