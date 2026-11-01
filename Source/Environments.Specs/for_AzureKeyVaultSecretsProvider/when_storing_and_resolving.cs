// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_AzureKeyVaultSecretsProvider;

public class when_storing_and_resolving : Specification
{
    InMemoryKeyVault _vault;
    ConfigEntry _entry;
    string _resolved;
    Exception _missing;
    int _storedCount;
    string _firstName;
    string _secondName;

    async Task Because()
    {
        _vault = new InMemoryKeyVault();
        var provider = new AzureKeyVaultSecretsProvider("production", _vault);
        _entry = await provider.StoreAsync("smtp:password", "value");
        _storedCount = _vault.Values.Count;
        _resolved = await provider.ResolveAsync("smtp:password", _entry);
        _missing = await Catch.Exception(() => provider.ResolveAsync("other", new ConfigEntry(ConfigEntryKind.Reference, "missing")));
        _firstName = AzureKeyVaultSecretsProvider.NameFor("production", "a:b");
        _secondName = AzureKeyVaultSecretsProvider.NameFor("production", "a-b");
        await provider.RemoveAsync("smtp:password", _entry);
    }

    [Fact] void should_store_the_value_in_the_vault() => _storedCount.ShouldEqual(1);
    [Fact] void should_delete_the_value_on_removal() => _vault.Values.ShouldBeEmpty();
    [Fact] void should_hand_back_a_reference() => _entry.Kind.ShouldEqual(ConfigEntryKind.Reference);
    [Fact] void should_not_put_the_value_in_the_entry() => _entry.Value.ShouldNotContain("value");
    [Fact] void should_resolve_the_value() => _resolved.ShouldEqual("value");
    [Fact] void should_use_a_valid_key_vault_name() => _entry.Value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-').ShouldBeTrue();
    [Fact] void should_keep_colliding_looking_keys_apart() => _firstName.ShouldNotEqual(_secondName);
    [Fact] void should_fail_clearly_when_the_secret_is_missing() => _missing.ShouldBeOfExactType<SecretNotAvailableException>();
}
