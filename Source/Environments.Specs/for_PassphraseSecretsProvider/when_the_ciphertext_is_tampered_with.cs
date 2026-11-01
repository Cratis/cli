// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_PassphraseSecretsProvider;

public class when_the_ciphertext_is_tampered_with : given.a_temp_project
{
    Exception _error;
    Exception _moved;

    async Task Because()
    {
        var environment = ProjectEnvironment.Create(_environments, "production", null, PassphraseSources.Fixed("p"), FastKdf);
        await environment.SetAsync("a", "value of a", secret: true);
        await environment.SetAsync("b", "value of b", secret: true);

        var document = _environments.Load("production");
        var bytes = Convert.FromBase64String(document.Config["a"].Value);
        bytes[^1] ^= 0x01;
        var tampered = new EnvironmentDocument { EncryptionSalt = document.EncryptionSalt, Kdf = document.Kdf, EncryptionCheck = document.EncryptionCheck };
        tampered.Config["a"] = new ConfigEntry(ConfigEntryKind.Secure, Convert.ToBase64String(bytes));
        tampered.Config["a-copy-of-b"] = document.Config["b"];

        var provider = new PassphraseSecretsProvider(tampered, PassphraseSources.Fixed("p"));
        _error = await Catch.Exception(() => provider.ResolveAsync("a", tampered.Config["a"]));
        _moved = await Catch.Exception(() => provider.ResolveAsync("a-copy-of-b", tampered.Config["a-copy-of-b"]));
    }

    [Fact] void should_refuse_the_modified_secret() => _error.ShouldBeOfExactType<SecretNotAvailableException>();
    [Fact] void should_refuse_a_secret_moved_to_another_key() => _moved.ShouldBeOfExactType<SecretNotAvailableException>();
}
