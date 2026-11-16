// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment.given;

public class a_vault_move : a_temp_project
{
    protected const string Key = "smtp:password";
    protected const string Value = "fixture-value";
    protected ProjectEnvironment _environment;
    protected VaultStorage _vault;
    protected string _resolved;
    protected string _originalLocation;
    string? _previousToken;
    string? _previousNamespace;

    void Establish()
    {
        _previousToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        _previousNamespace = Environment.GetEnvironmentVariable("VAULT_NAMESPACE");
        Environment.SetEnvironmentVariable("VAULT_TOKEN", "fixture-token");
        Environment.SetEnvironmentVariable("VAULT_NAMESPACE", null);
        _vault = new VaultStorage();
    }

    protected async Task Create(string provider)
    {
        _environment = ProjectEnvironment.Create(_environments, "production", provider, PassphraseSources.Fixed("p"), vaultHandler: _vault);
        await _environment.SetAsync(Key, Value, secret: true);
        _originalLocation = _vault.Values.Keys.Single();
    }

    protected async Task Move(string provider)
    {
        await _environment.ChangeSecretsProviderAsync(provider);
        _resolved = await _environment.GetValueAsync(Key);
    }

    void Destroy()
    {
        _vault.Dispose();
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _previousToken);
        Environment.SetEnvironmentVariable("VAULT_NAMESPACE", _previousNamespace);
    }
}
