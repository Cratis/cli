// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// Stores and resolves the secrets of one environment.
/// </summary>
public interface ISecretsProvider
{
    /// <summary>
    /// Stores a secret and returns the entry that goes into the environment file. The entry never holds the clear value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The secret value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entry to persist.</returns>
    Task<ConfigEntry> StoreAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the clear value of a secret entry.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="entry">The entry from the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The clear value.</returns>
    Task<string> ResolveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the stored material for an entry. Inline entries need nothing removed.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="entry">The entry from the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task RemoveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default);
}
