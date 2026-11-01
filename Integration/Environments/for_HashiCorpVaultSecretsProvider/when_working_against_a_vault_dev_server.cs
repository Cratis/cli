// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments.Secrets;

namespace Cratis.Environments.Integration.for_HashiCorpVaultSecretsProvider;

/// <summary>
/// Runs the provider against a real <c language="csharp">hashicorp/vault</c> dev container. Skipped where no Docker daemon is available.
/// </summary>
/// <param name="vault">The shared Vault dev server.</param>
[Collection(VaultCollection.Name)]
public class when_working_against_a_vault_dev_server(VaultContainerFixture vault) : Specification
{
    const string SecretValue = "integration-secret-value";

    string? _previousToken;
    string _file = string.Empty;
    string? _resolved;
    string? _afterRemoval;
    string? _moved;
    string _tempRoot = string.Empty;

    async Task Because()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        _previousToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        Environment.SetEnvironmentVariable("VAULT_TOKEN", VaultContainerFixture.RootToken);
        _tempRoot = Path.Combine(Path.GetTempPath(), "cratis-vault-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        var environments = ProjectEnvironments.ForProject(_tempRoot);
        var spec = $"hashivault+http://{vault.Address}/secret/cratis";

        var environment = ProjectEnvironment.Create(environments, "production", spec, PassphraseSources.Fixed("unused"));
        await environment.SetAsync("smtp:password", SecretValue, secret: true);
        _file = await File.ReadAllTextAsync(environments.PathFor("production"));
        _resolved = await ProjectEnvironment.Open(environments, "production", PassphraseSources.Fixed("unused")).GetValueAsync("smtp:password");

        await environment.ChangeSecretsProviderAsync("passphrase");
        _moved = await ProjectEnvironment.Open(environments, "production", PassphraseSources.Fixed("unused")).GetValueAsync("smtp:password");
        var movedFile = await File.ReadAllTextAsync(environments.PathFor("production"));
        _afterRemoval = movedFile.Contains(SecretValue, StringComparison.Ordinal) ? movedFile : null;
    }

    void Destroy()
    {
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _previousToken);
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [DockerFact]
    void should_store_a_reference_and_no_clear_text_in_the_file() => (_file.Contains("ref:", StringComparison.Ordinal) && !_file.Contains(SecretValue, StringComparison.Ordinal)).ShouldBeTrue();

    [DockerFact]
    void should_resolve_the_secret_from_vault() => _resolved.ShouldEqual(SecretValue);

    [DockerFact]
    void should_move_the_secret_to_the_passphrase_provider() => _moved.ShouldEqual(SecretValue);

    [DockerFact]
    void should_leave_no_clear_text_in_the_file_after_moving() => _afterRemoval.ShouldBeNull();
}
