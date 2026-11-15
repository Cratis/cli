// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments.Secrets;

namespace Cratis.Environments;

internal sealed record SecretStore(SecretsProviderKind Kind, Uri? Endpoint, string Mount, string Namespace, string Specification)
{
    public static SecretStore From(SecretsProviderSpec spec, string? specification = null) => spec.Kind switch
    {
        SecretsProviderKind.AzureKeyVault => new(spec.Kind, AzureKeyVaultSecrets.ToUri(spec.Target), string.Empty, string.Empty, specification ?? spec.Text),
        SecretsProviderKind.HashiCorpVault => Vault(spec, specification ?? spec.Text),
        _ => new(spec.Kind, null, string.Empty, string.Empty, specification ?? spec.Text)
    };

    public bool CanRemove(ConfigEntry previous, ConfigEntry next, SecretStore target)
    {
        if (Kind != target.Kind)
        {
            return true;
        }

        if (Kind == SecretsProviderKind.AzureKeyVault && Endpoint!.IdnHost != target.Endpoint!.IdnHost)
        {
            return true;
        }

        // A different Vault address might be an alias or load balancer for the same server.
        if (Endpoint != target.Endpoint)
        {
            return false;
        }

        return Mount != target.Mount || Namespace != target.Namespace || previous != next;
    }

    public string? KeptWarning(string key, ConfigEntry entry, SecretStore target) => this == target
        ? null
        : $"Kept previous {Kind} entry '{entry.Value}' for key '{key}' at {Endpoint} (mount '{Mount}', namespace '{Namespace}'): the stores may be the same. Remove it manually only after verifying it is separate from the new entry.";

    static SecretStore Vault(SecretsProviderSpec spec, string specification)
    {
        var parts = spec.Target.Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
        var scheme = spec.Text.StartsWith("hashivault+http://", StringComparison.OrdinalIgnoreCase) ? "http" : "https";
        var endpoint = new Uri($"{scheme}://{parts[0]}/");
        var ns = Environment.GetEnvironmentVariable("VAULT_NAMESPACE")?.Trim('/') ?? string.Empty;
        return new(spec.Kind, endpoint, parts[1], ns, specification);
    }
}
