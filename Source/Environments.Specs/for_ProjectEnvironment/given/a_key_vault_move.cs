// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.for_ProjectEnvironment.given;

public class a_key_vault_move : a_temp_project
{
    protected const string Key = "smtp:password";
    protected const string Value = "fixture-value";
    protected ProjectEnvironment _environment;
    protected InMemoryKeyVault _source;
    protected InMemoryKeyVault _destination;
    protected string _resolved;
    protected string _reference;
    IKeyVaultSecrets _access;

    void Establish()
    {
        _source = new InMemoryKeyVault();
        _destination = _source;

        // One injected adapter serves both providers. Route writes to the chosen destination,
        // while reads and cleanup use the source until the move has completed.
        _access = Substitute.For<IKeyVaultSecrets>();
        _access.SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _destination.SetAsync(call.ArgAt<string>(0), call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));
        _access.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _source.GetAsync(call.ArgAt<string>(0), call.ArgAt<CancellationToken>(1)));
        _access.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _source.DeleteAsync(call.ArgAt<string>(0), call.ArgAt<CancellationToken>(1)));
    }

    protected async Task Create(string provider)
    {
        _environment = ProjectEnvironment.Create(_environments, "production", provider, PassphraseSources.Fixed("p"), keyVault: _access);
        await _environment.SetAsync(Key, Value, secret: true);
        _reference = _environment.GetEntry(Key)!.Value;
    }

    protected async Task Move(string provider)
    {
        await _environment.ChangeSecretsProviderAsync(provider);
        _resolved = await ProjectEnvironment.Open(_environments, "production", PassphraseSources.Fixed("p"), _destination).GetValueAsync(Key);
    }
}
