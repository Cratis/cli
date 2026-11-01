// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.given;

public class InMemoryKeyVault : IKeyVaultSecrets
{
    public Dictionary<string, string> Values { get; } = [];

    public Task SetAsync(string name, string value, CancellationToken cancellationToken)
    {
        Values[name] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Values.TryGetValue(name, out var value) ? value : null);

    public Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        Values.Remove(name);
        return Task.CompletedTask;
    }
}
