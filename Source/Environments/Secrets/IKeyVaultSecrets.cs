// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// The Azure Key Vault operations the provider needs, behind an abstraction so it can be specified without Azure.
/// </summary>
public interface IKeyVaultSecrets
{
    /// <summary>
    /// Sets a secret.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task SetAsync(string name, string value, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a secret.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value, or null when the secret does not exist.</returns>
    Task<string?> GetAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a secret.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task DeleteAsync(string name, CancellationToken cancellationToken);
}
