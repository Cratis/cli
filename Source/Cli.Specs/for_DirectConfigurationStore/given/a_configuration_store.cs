// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectConfigurationStore.given;

public class a_configuration_store : Specification
{
    protected readonly TaskCompletionSource FirstInside = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource ReleaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private protected readonly a_fake_authorization_server Server = new();
    private protected DirectConfigurationStore First = null!;
    private protected DirectConfigurationStore Second = null!;
    protected CliConfiguration Persisted = new() { ActiveContext = "concurrent-context", Direct = new DirectConfiguration() };
    string _home = null!;
    string _json = null!;

    void Establish()
    {
        _home = Path.Combine(Directory.GetCurrentDirectory(), ".ai-work", "direct-config-spec-" + Guid.NewGuid().ToString("N"));
        _json = JsonSerializer.Serialize(Persisted);
        First = Create();
        Second = Create();
    }

    private protected Task<string?> Login(DirectConfigurationStore store, string tenant, bool pause) => store.Update(
    async (config, save) =>
    {
        if (pause)
        {
            FirstInside.SetResult();
            await ReleaseFirst.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        var target = DirectTarget.Create("https://direct.example", tenant);
        var provider = Server.Provider();
        return await provider.Replace(target, new DirectTokens("access", "refresh-" + tenant, DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None, () =>
        {
            DirectCredentials.Record(config.Direct!, Entry(tenant));
            save();
        });
    },
    CancellationToken.None);

    private protected static DirectCredentialEntry Entry(string tenant) => new() { Origin = "https://direct.example", Tenant = tenant, Issuer = "https://identity.example/" };

    DirectConfigurationStore Create() => new(new DirectRefreshLock(_home), () => JsonSerializer.Deserialize<CliConfiguration>(_json)!, config =>
    {
        _json = JsonSerializer.Serialize(config);
        Persisted = JsonSerializer.Deserialize<CliConfiguration>(_json)!;
    });

    void Destroy()
    {
        Server.Dispose();
        if (Directory.Exists(_home))
        {
            Directory.Delete(_home, recursive: true);
        }
    }
}
