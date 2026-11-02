// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_PassphraseSecretsProvider;

public class when_two_environments_share_a_passphrase : a_temp_project
{
    EnvironmentDocument _first;
    EnvironmentDocument _second;

    async Task Because()
    {
        var passphrase = PassphraseSources.Fixed("same passphrase");
        var one = ProjectEnvironment.Create(_environments, "one", null, passphrase, FastKdf);
        var two = ProjectEnvironment.Create(_environments, "two", null, passphrase, FastKdf);
        await one.SetAsync("key", "same value", secret: true);
        await two.SetAsync("key", "same value", secret: true);
        _first = _environments.Load("one");
        _second = _environments.Load("two");
    }

    [Fact] void should_use_different_salts() => _first.EncryptionSalt.ShouldNotEqual(_second.EncryptionSalt);
    [Fact] void should_produce_different_ciphertext() => _first.Config["key"].Value.ShouldNotEqual(_second.Config["key"].Value);
    [Fact] void should_produce_different_verification_values() => _first.EncryptionCheck.ShouldNotEqual(_second.EncryptionCheck);
}
