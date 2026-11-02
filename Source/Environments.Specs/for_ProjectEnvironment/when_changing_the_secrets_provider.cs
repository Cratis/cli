// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment;

public class when_changing_the_secrets_provider : a_temp_project
{
    const string SecretValue = "moving-secret-value";

    InMemoryKeyVault _vault;
    string _afterToVault;
    string _afterBack;
    string _resolvedFromVault;
    string _resolvedBack;
    int _vaultCountAfterBack;

    async Task Because()
    {
        _vault = new InMemoryKeyVault();
        var environment = ProjectEnvironment.Create(_environments, "production", null, PassphraseSources.Fixed("p"), FastKdf, _vault);
        await environment.SetAsync("smtp:password", SecretValue, secret: true);

        await environment.ChangeSecretsProviderAsync("azurekeyvault://my-vault");
        _afterToVault = FileText("production");
        _resolvedFromVault = await ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("p"), _vault).GetValueAsync("smtp:password");

        await environment.ChangeSecretsProviderAsync("passphrase");
        _afterBack = FileText("production");
        _vaultCountAfterBack = _vault.Values.Count;
        _resolvedBack = await ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("p"), _vault).GetValueAsync("smtp:password");
    }

    [Fact] void should_record_the_new_provider() => _afterToVault.ShouldContain("azurekeyvault://my-vault");
    [Fact] void should_leave_no_clear_text_in_the_file_after_moving_to_the_vault() => _afterToVault.ShouldNotContain(SecretValue);
    [Fact] void should_leave_no_ciphertext_in_the_file_after_moving_to_the_vault() => _afterToVault.ShouldNotContain("secure:");
    [Fact] void should_hold_a_reference_after_moving_to_the_vault() => _afterToVault.ShouldContain("ref:");
    [Fact] void should_resolve_through_the_vault() => _resolvedFromVault.ShouldEqual(SecretValue);
    [Fact] void should_leave_no_clear_text_in_the_file_after_moving_back() => _afterBack.ShouldNotContain(SecretValue);
    [Fact] void should_remove_the_vault_material_after_moving_back() => _vaultCountAfterBack.ShouldEqual(0);
    [Fact] void should_resolve_after_moving_back() => _resolvedBack.ShouldEqual(SecretValue);
}
